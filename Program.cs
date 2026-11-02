using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using StudyPlanner.Data;
using StudyPlanner.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/ index.html";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddHostedService<ReminderBackgroundService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
