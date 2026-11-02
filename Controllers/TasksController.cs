using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyPlanner.Data;
using StudyPlanner.DTOs;
using StudyPlanner.Models;

namespace StudyPlanner.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Student")]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _db;

    public TasksController(AppDbContext db) => _db = db;

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetMyTasks(
        [FromQuery] string? search,
        [FromQuery] int? disciplineId,
        [FromQuery] int? priorityId,
        [FromQuery] int? statusId)
    {
        var query = _db.Tasks
            .Include(t => t.Priority)
            .Include(t => t.Status)
            .Include(t => t.TaskType)
            .Include(t => t.Discipline)
            .Include(t => t.Schedule)
            .Where(t => t.UserId == CurrentUserId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(t =>
                t.Title.ToLower().Contains(s) ||
                (t.Description != null && t.Description.ToLower().Contains(s)) ||
                t.Discipline!.Name.ToLower().Contains(s));
        }

        if (disciplineId.HasValue)
            query = query.Where(t => t.DisciplineId == disciplineId.Value);

        if (priorityId.HasValue)
            query = query.Where(t => t.PriorityId == priorityId.Value);

        if (statusId.HasValue)
            query = query.Where(t => t.StatusId == statusId.Value);

        var tasks = await query
            .OrderBy(t => t.Status!.IsFinal)
            .ThenByDescending(t => t.Priority!.Level)
            .ThenBy(t => t.Deadline)
            .ToListAsync();

        return Ok(tasks);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTask([FromBody] UpsertTaskRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest(new { message = "Наименование задачи обязательно." });

        var task = new StudyTask
        {
            Title = req.Title.Trim(),
            Description = req.Description?.Trim(),
            Deadline = req.Deadline,
            PriorityId = req.PriorityId,
            StatusId = req.StatusId,
            TypeId = req.TypeId,
            DisciplineId = req.DisciplineId,
            ScheduleId = req.ScheduleId,
            UserId = CurrentUserId,
            CreatedAt = DateTime.UtcNow
        };

        var status = await _db.Statuses.FindAsync(req.StatusId);
        if (status?.IsFinal == true)
            task.CompletedAt = DateTime.UtcNow;

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();

        // Автоматическая генерация напоминаний (за 3 дня, за 24 часа и в день дедлайна)
        var reminderTypes = await _db.ReminderTypes.ToListAsync();
        foreach (var rt in reminderTypes)
        {
            var scheduledTime = task.Deadline.AddSeconds(-rt.OffsetSeconds);
            if (scheduledTime > DateTime.UtcNow)
            {
                _db.Reminders.Add(new Reminder
                {
                    TaskId = task.Id,
                    UserId = CurrentUserId,
                    ReminderTypeId = rt.Id,
                    ScheduledAt = scheduledTime,
                    IsSent = false
                });
            }
        }
        await _db.SaveChangesAsync();

        return Ok(task);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateTask(int id, [FromBody] UpsertTaskRequest req)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == CurrentUserId);
        if (task == null)
            return NotFound(new { message = "Задача не найдена." });

        task.Title = req.Title.Trim();
        task.Description = req.Description?.Trim();
        task.Deadline = req.Deadline;
        task.PriorityId = req.PriorityId;
        task.StatusId = req.StatusId;
        task.TypeId = req.TypeId;
        task.DisciplineId = req.DisciplineId;
        task.ScheduleId = req.ScheduleId;

        var status = await _db.Statuses.FindAsync(req.StatusId);
        if (status?.IsFinal == true && task.CompletedAt == null)
        {
            task.CompletedAt = DateTime.UtcNow;
            var pending = await _db.Reminders.Where(r => r.TaskId == task.Id && !r.IsSent).ToListAsync();
            _db.Reminders.RemoveRange(pending);
        }
        else if (status?.IsFinal == false)
        {
            task.CompletedAt = null;
        }

        await _db.SaveChangesAsync();
        return Ok(task);
    }

    [HttpPatch("{id:int}/status/{statusId:int}")]
    public async Task<IActionResult> ChangeStatus(int id, int statusId)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == CurrentUserId);
        if (task == null)
            return NotFound();

        var status = await _db.Statuses.FindAsync(statusId);
        if (status == null)
            return BadRequest();

        task.StatusId = statusId;
        task.CompletedAt = status.IsFinal ? DateTime.UtcNow : null;

        await _db.SaveChangesAsync();
        return Ok(task);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == CurrentUserId);
        if (task == null)
            return NotFound();

        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
