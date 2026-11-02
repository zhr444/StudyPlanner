using Microsoft.EntityFrameworkCore;
using StudyPlanner.Models;

namespace StudyPlanner.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Priority> Priorities => Set<Priority>();
    public DbSet<Status> Statuses => Set<Status>();
    public DbSet<TaskType> TaskTypes => Set<TaskType>();
    public DbSet<Discipline> Disciplines => Set<Discipline>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<TeacherDiscipline> TeacherDisciplines => Set<TeacherDiscipline>();
    public DbSet<StudyGroup> Groups => Set<StudyGroup>();
    public DbSet<GroupDiscipline> GroupDisciplines => Set<GroupDiscipline>();
    public DbSet<ScheduleItem> Schedule => Set<ScheduleItem>();
    public DbSet<User> Users => Set<User>();
    public DbSet<AdminAccount> Admins => Set<AdminAccount>();
    public DbSet<StudyTask> Tasks => Set<StudyTask>();
    public DbSet<ReminderType> ReminderTypes => Set<ReminderType>();
    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<ReportExport> ReportExports => Set<ReportExport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TeacherDiscipline>()
            .HasKey(td => new { td.TeacherId, td.DisciplineId });

        modelBuilder.Entity<GroupDiscipline>()
            .HasKey(gd => new { gd.GroupId, gd.DisciplineId });

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<AdminAccount>()
            .HasIndex(a => a.Login)
            .IsUnique();
    }
}
