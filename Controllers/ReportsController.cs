using System.Security.Claims;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StudyPlanner.Data;
using StudyPlanner.Models;

namespace StudyPlanner.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Student")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ReportsController(AppDbContext db) => _db = db;

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("export")]
    public async Task<IActionResult> ExportReport(
        [FromQuery] DateOnly periodFrom,
        [FromQuery] DateOnly periodTo,
        [FromQuery] string format = "XLSX")
    {
        var normalizedFormat = format.ToUpperInvariant() == "PDF" ? "PDF" : "XLSX";
        var startDt = periodFrom.ToDateTime(TimeOnly.MinValue);
        var endDt = periodTo.ToDateTime(TimeOnly.MaxValue);

        var user = await _db.Users.Include(u => u.Group).FirstAsync(u => u.Id == CurrentUserId);
        var tasks = await _db.Tasks
            .Include(t => t.Discipline)
            .Include(t => t.TaskType)
            .Include(t => t.Priority)
            .Include(t => t.Status)
            .Where(t => t.UserId == CurrentUserId && t.Deadline >= startDt && t.Deadline <= endDt)
            .OrderBy(t => t.Deadline)
            .ToListAsync();

        // Фиксация выгрузки в таблице report_exports
        _db.ReportExports.Add(new ReportExport
        {
            UserId = CurrentUserId,
            Format = normalizedFormat,
            PeriodFrom = periodFrom,
            PeriodTo = periodTo,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        if (normalizedFormat == "XLSX")
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Отчёт об успеваемости");

            ws.Cell(1, 1).Value = $"Отчёт об успеваемости студента: {user.LastName} {user.FirstName} ({user.Group?.Name})";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(2, 1).Value = $"Период: {periodFrom:dd.MM.yyyy} – {periodTo:dd.MM.yyyy}";

            string[] headers = { "№", "Наименование задачи", "Дисциплина", "Тип работы", "Приоритет", "Статус", "Дедлайн", "Дата сдачи" };
            for (int c = 0; c < headers.Length; c++)
            {
                ws.Cell(4, c + 1).Value = headers[c];
                ws.Cell(4, c + 1).Style.Font.Bold = true;
                ws.Cell(4, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
                ws.Cell(4, c + 1).Style.Font.FontColor = XLColor.White;
            }

            for (int i = 0; i < tasks.Count; i++)
            {
                var t = tasks[i];
                int r = i + 5;
                ws.Cell(r, 1).Value = i + 1;
                ws.Cell(r, 2).Value = t.Title;
                ws.Cell(r, 3).Value = t.Discipline?.Name ?? "-";
                ws.Cell(r, 4).Value = t.TaskType?.Name ?? "-";
                ws.Cell(r, 5).Value = t.Priority?.Name ?? "-";
                ws.Cell(r, 6).Value = t.Status?.Name ?? "-";
                ws.Cell(r, 7).Value = t.Deadline.ToString("dd.MM.yyyy HH:mm");
                ws.Cell(r, 8).Value = t.CompletedAt?.ToString("dd.MM.yyyy HH:mm") ?? "—";
            }

            ws.Columns().AdjustToContents();
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"report_{periodFrom:yyyyMMdd}_{periodTo:yyyyMMdd}.xlsx");
        }
        else
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.Header().Text($"Отчёт об успеваемости: {user.LastName} {user.FirstName} ({user.Group?.Name})")
                        .FontSize(16).SemiBold().FontColor(Colors.Blue.Darken3);
                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Item().Text($"Период отчёта: {periodFrom:dd.MM.yyyy} – {periodTo:dd.MM.yyyy}").FontSize(11);
                        col.Item().PaddingTop(8).Text($"Всего задач: {tasks.Count} | Сдано: {tasks.Count(x => x.Status?.IsFinal == true)}").FontSize(11).Bold();
                    });
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", $"report_{periodFrom:yyyyMMdd}_{periodTo:yyyyMMdd}.pdf");
        }
    }
}
