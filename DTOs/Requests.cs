namespace StudyPlanner.DTOs;

public record RegisterRequest(
    string LastName,
    string FirstName,
    string? Patronymic,
    string Email,
    string? Phone,
    int GroupId,
    int Course,
    string Password,
    string ConfirmPassword
);

public record LoginRequest(string Email, string Password);

public record AdminLoginRequest(string Login, string Password);

public record UpdateProfileRequest(
    string LastName,
    string FirstName,
    string? Patronymic,
    string Email,
    string? Phone,
    int GroupId,
    string? NewPassword
);

public record UpsertTaskRequest(
    string Title,
    string? Description,
    DateTime Deadline,
    int PriorityId,
    int StatusId,
    int TypeId,
    int DisciplineId,
    int? ScheduleId
);

public record BlockUserRequest(bool IsBlocked, string? BlockReason);
