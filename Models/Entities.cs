using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudyPlanner.Models;

[Table("priorities")]
public class Priority
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required, MaxLength(20)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("level")]
    public int Level { get; set; }

    [Required, MaxLength(7)]
    [Column("color")]
    public string Color { get; set; } = "#3B82F6";
}

[Table("statuses")]
public class Status
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required, MaxLength(30)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(7)]
    [Column("color")]
    public string Color { get; set; } = "#64748B";

    [Column("is_final")]
    public bool IsFinal { get; set; }
}

[Table("task_types")]
public class TaskType
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required, MaxLength(50)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;
}

[Table("disciplines")]
public class Discipline
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required, MaxLength(150)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }
}

[Table("teachers")]
public class Teacher
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("patronymic")]
    public string? Patronymic { get; set; }

    [MaxLength(255)]
    [Column("email")]
    public string? Email { get; set; }

    [MaxLength(20)]
    [Column("phone")]
    public string? Phone { get; set; }

    [MaxLength(100)]
    [Column("degree")]
    public string? Degree { get; set; }
}

[Table("teacher_disciplines")]
public class TeacherDiscipline
{
    [Column("teacher_id")]
    public int TeacherId { get; set; }
    public Teacher? Teacher { get; set; }

    [Column("discipline_id")]
    public int DisciplineId { get; set; }
    public Discipline? Discipline { get; set; }
}

[Table("groups")]
public class StudyGroup
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required, MaxLength(20)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("course")]
    public int Course { get; set; }

    [Column("start_year")]
    public int StartYear { get; set; }
}

[Table("group_disciplines")]
public class GroupDiscipline
{
    [Column("group_id")]
    public int GroupId { get; set; }
    public StudyGroup? Group { get; set; }

    [Column("discipline_id")]
    public int DisciplineId { get; set; }
    public Discipline? Discipline { get; set; }

    [Column("teacher_id")]
    public int TeacherId { get; set; }
    public Teacher? Teacher { get; set; }
}

[Table("schedule")]
public class ScheduleItem
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("group_id")]
    public int GroupId { get; set; }
    public StudyGroup? Group { get; set; }

    [Column("discipline_id")]
    public int DisciplineId { get; set; }
    public Discipline? Discipline { get; set; }

    [Column("teacher_id")]
    public int TeacherId { get; set; }
    public Teacher? Teacher { get; set; }

    [Column("day_of_week")]
    public int DayOfWeek { get; set; }

    [Column("pair_number")]
    public int PairNumber { get; set; }

    [Column("is_even_week")]
    public bool IsEvenWeek { get; set; }

    [Required, MaxLength(20)]
    [Column("audience")]
    public string Audience { get; set; } = string.Empty;

    [Column("start_time")]
    public TimeOnly StartTime { get; set; }

    [Column("end_time")]
    public TimeOnly EndTime { get; set; }
}

[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("patronymic")]
    public string? Patronymic { get; set; }

    [Required, MaxLength(255)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    [Column("phone")]
    public string? Phone { get; set; }

    [Required, MaxLength(255)]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("group_id")]
    public int GroupId { get; set; }
    public StudyGroup? Group { get; set; }

    [Column("registration_date")]
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

    [Column("is_admin")]
    public bool IsAdmin { get; set; }

    [Column("is_blocked")]
    public bool IsBlocked { get; set; }

    [Column("block_reason")]
    public string? BlockReason { get; set; }
}

[Table("admins")]
public class AdminAccount
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required, MaxLength(255)]
    [Column("login")]
    public string Login { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;

    [Column("is_super_admin")]
    public bool IsSuperAdmin { get; set; }

    [Column("last_login_at")]
    public DateTime? LastLoginAt { get; set; }
}

[Table("tasks")]
public class StudyTask
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required, MaxLength(200)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("deadline")]
    public DateTime Deadline { get; set; }

    [Column("priority_id")]
    public int PriorityId { get; set; }
    public Priority? Priority { get; set; }

    [Column("status_id")]
    public int StatusId { get; set; }
    public Status? Status { get; set; }

    [Column("type_id")]
    public int TypeId { get; set; }
    public TaskType? TaskType { get; set; }

    [Column("discipline_id")]
    public int DisciplineId { get; set; }
    public Discipline? Discipline { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }
    public User? User { get; set; }

    [Column("schedule_id")]
    public int? ScheduleId { get; set; }
    public ScheduleItem? Schedule { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }
}

[Table("reminder_types")]
public class ReminderType
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required, MaxLength(50)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("offset_seconds")]
    public int OffsetSeconds { get; set; }
}

[Table("reminders")]
public class Reminder
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("task_id")]
    public int TaskId { get; set; }
    public StudyTask? Task { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }
    public User? User { get; set; }

    [Column("reminder_type_id")]
    public int ReminderTypeId { get; set; }
    public ReminderType? ReminderType { get; set; }

    [Column("scheduled_at")]
    public DateTime ScheduledAt { get; set; }

    [Column("sent_at")]
    public DateTime? SentAt { get; set; }

    [Column("is_sent")]
    public bool IsSent { get; set; }
}

[Table("report_exports")]
public class ReportExport
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }
    public User? User { get; set; }

    [Required, MaxLength(10)]
    [Column("format")]
    public string Format { get; set; } = "XLSX";

    [Column("period_from")]
    public DateOnly PeriodFrom { get; set; }

    [Column("period_to")]
    public DateOnly PeriodTo { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
