using Microsoft.EntityFrameworkCore;
using StudyPlanner.Data;

namespace StudyPlanner.Services;

public class ReminderBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ReminderBackgroundService> _logger;

    public ReminderBackgroundService(IServiceProvider services, ILogger<ReminderBackgroundService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var dueReminders = await db.Reminders
                    .Include(r => r.Task)
                    .Include(r => r.ReminderType)
                    .Include(r => r.User)
                    .Where(r => !r.IsSent && r.ScheduledAt <= DateTime.UtcNow)
                    .ToListAsync(stoppingToken);

                foreach (var r in dueReminders)
                {
                    r.IsSent = true;
                    r.SentAt = DateTime.UtcNow;
                    _logger.LogInformation(
                        "Отправлено напоминание [{Type}] студенту {Email} по задаче '{TaskTitle}' (дедлайн: {Deadline})",
                        r.ReminderType?.Name, r.User?.Email, r.Task?.Title, r.Task?.Deadline);
                }

                if (dueReminders.Count > 0)
                    await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке очереди напоминаний.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
