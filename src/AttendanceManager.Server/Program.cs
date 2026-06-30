using System.Text;
using AttendanceManager.Core.Interfaces;
using AttendanceManager.Infrastructure.Data;
using AttendanceManager.Infrastructure.Repositories;
using AttendanceManager.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/server-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
    .CreateLogger();

try
{
    Log.Information("Starting AttendanceManager Server");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    var jwtKey = builder.Configuration["Jwt:Key"] ?? "AttendanceManager_SuperSecretKey_2024_ChangeInProduction!";
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Server=localhost;Database=AttendanceManagerDb;Trusted_Connection=True;TrustServerCertificate=True;";

    // Database
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(connectionString));

    // Repositories
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

    // Services
    builder.Services.AddScoped<IAuditService, AuditService>();
    builder.Services.AddScoped<IAttendanceService, AttendanceService>();
    builder.Services.AddScoped<IEmployeeService, EmployeeService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<ILeaveService, LeaveService>();
    builder.Services.AddScoped<IReportService, ReportService>();

    // JWT Authentication
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateIssuer = true,
                ValidIssuer = "AttendanceManager",
                ValidateAudience = true,
                ValidAudience = "AttendanceManager",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    });

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
    });

    var app = builder.Build();

    // Auto-migrate database
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors("AllowAll");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
