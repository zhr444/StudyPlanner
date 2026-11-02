using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyPlanner.Data;
using StudyPlanner.DTOs;
using StudyPlanner.Models;

namespace StudyPlanner.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminController(AppDbContext db) => _db = db;

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] int? groupId, [FromQuery] int? statusId)
    {
        var query = _db.Users.Include(u => u.Group).AsQueryable();
        if (groupId.HasValue)
            query = query.Where(u => u.GroupId == groupId.Value);

        var users = await query
            .Select(u => new
            {
                u.Id,
                u.LastName,
                u.FirstName,
                u.Patronymic,
                u.Email,
                u.Phone,
                GroupName = u.Group!.Name,
                u.Group.Course,
                u.RegistrationDate,
                u.IsBlocked,
                u.BlockReason,
                TotalTasks = _db.Tasks.Count(t => t.UserId == u.Id && (!statusId.HasValue || t.StatusId == statusId.Value)),
                CompletedTasks = _db.Tasks.Count(t => t.UserId == u.Id && t.Status!.IsFinal),
                OverdueTasks = _db.Tasks.Count(t => t.UserId == u.Id && t.StatusId == 4)
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPatch("users/{id:int}/block")]
    public async Task<IActionResult> BlockOrUnblockUser(int id, [FromBody] BlockUserRequest req)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        user.IsBlocked = req.IsBlocked;
        user.BlockReason = req.IsBlocked ? req.BlockReason : null;
        await _db.SaveChangesAsync();

        return Ok(user);
    }

    [HttpPost("users/{id:int}/reset-password")]
    public async Task<IActionResult> ResetStudentPassword(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        user.PasswordHash = AuthController.ComputeSha256("Student123!");
        await _db.SaveChangesAsync();

        return Ok(new { message = "Пароль пользователя сброшен на временный: Student123!" });
    }

    [HttpPost("disciplines")]
    public async Task<IActionResult> AddDiscipline([FromBody] Discipline model)
    {
        _db.Disciplines.Add(model);
        await _db.SaveChangesAsync();
        return Ok(model);
    }

    [HttpDelete("disciplines/{id:int}")]
    public async Task<IActionResult> DeleteDiscipline(int id)
    {
        var d = await _db.Disciplines.FindAsync(id);
        if (d == null) return NotFound();
        _db.Disciplines.Remove(d);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("teachers")]
    public async Task<IActionResult> AddTeacher([FromBody] Teacher model)
    {
        _db.Teachers.Add(model);
        await _db.SaveChangesAsync();
        return Ok(model);
    }

    [HttpPost("schedule")]
    public async Task<IActionResult> AddScheduleItem([FromBody] ScheduleItem model)
    {
        _db.Schedule.Add(model);
        await _db.SaveChangesAsync();
        return Ok(model);
    }
}
