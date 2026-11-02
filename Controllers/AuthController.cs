using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyPlanner.Data;
using StudyPlanner.DTOs;
using StudyPlanner.Models;

namespace StudyPlanner.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;

    public AuthController(AppDbContext db) => _db = db;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.LastName) || string.IsNullOrWhiteSpace(req.FirstName))
            return BadRequest(new { message = "Фамилия и имя обязательны для заполнения." });

        if (req.Password != req.ConfirmPassword)
            return BadRequest(new { message = "Пароль и подтверждение пароля не совпадают." });

        if (req.Password.Length < 6)
            return BadRequest(new { message = "Пароль должен содержать не менее 6 символов." });

        bool isValidEmail = Regex.IsMatch(req.Email ?? "", @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        if (!isValidEmail)
            return BadRequest(new { message = "Указан некорректный формат электронной почты." });

        bool exists = await _db.Users.AnyAsync(u =>
            u.Email.ToLower() == req.Email.ToLower() ||
            (!string.IsNullOrEmpty(req.Phone) && u.Phone == req.Phone));

        if (exists)
            return Conflict(new { message = "Учётная запись с таким email или телефоном уже существует." });

        var user = new User
        {
            LastName = req.LastName.Trim(),
            FirstName = req.FirstName.Trim(),
            Patronymic = string.IsNullOrWhiteSpace(req.Patronymic) ? null : req.Patronymic.Trim(),
            Email = req.Email.Trim().ToLower(),
            Phone = req.Phone?.Trim(),
            GroupId = req.GroupId,
            PasswordHash = ComputeSha256(req.Password),
            RegistrationDate = DateTime.UtcNow,
            IsAdmin = false,
            IsBlocked = false
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Регистрация успешно завершена.", userId = user.Id });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var hash = ComputeSha256(req.Password);
        var user = await _db.Users
            .Include(u => u.Group)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == req.Email.ToLower() && u.PasswordHash == hash);

        if (user == null)
            return Unauthorized(new { message = "Неверный логин (email) или пароль." });

        if (user.IsBlocked)
            return StatusCode(403, new { message = $"Учётная запись заблокирована. Причина: {user.BlockReason ?? "Обратитесь к администратору"}" });

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, $"{user.LastName} {user.FirstName}"),
            new(ClaimTypes.Role, "Student"),
            new("GroupId", user.GroupId.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return Ok(new
        {
            user.Id,
            user.LastName,
            user.FirstName,
            user.Patronymic,
            user.Email,
            user.Phone,
            user.GroupId,
            GroupName = user.Group?.Name,
            Course = user.Group?.Course
        });
    }

    [HttpPost("admin-login")]
    public async Task<IActionResult> AdminLogin([FromBody] AdminLoginRequest req)
    {
        var hash = ComputeSha256(req.Password);
        var admin = await _db.Admins.FirstOrDefaultAsync(a => a.Login == req.Login && a.PasswordHash == hash);

        if (admin == null)
            return Unauthorized(new { message = "Неверные учётные данные администратора." });

        admin.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, admin.Id.ToString()),
            new(ClaimTypes.Name, admin.FullName),
            new(ClaimTypes.Role, "Admin")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return Ok(new { admin.Id, admin.Login, admin.FullName, admin.IsSuperAdmin });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { message = "Выход выполнен." });
    }

    public static string ComputeSha256(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
