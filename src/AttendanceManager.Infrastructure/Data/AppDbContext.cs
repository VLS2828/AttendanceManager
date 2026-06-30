using AttendanceManager.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AttendanceManager.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<Holiday> Holidays => Set<Holiday>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<IdleLog> IdleLogs => Set<IdleLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Employee
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.EmployeeCode).IsUnique();
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(200).IsRequired();
            entity.Property(e => e.EmployeeCode).HasMaxLength(20).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Ignore(e => e.FullName);

            entity.HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Department
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(d => d.Name).IsUnique();
        });

        // Attendance
        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => new { a.EmployeeId, a.Date }).IsUnique();
            entity.Property(a => a.ComputerName).HasMaxLength(100);
            entity.Property(a => a.WindowsUsername).HasMaxLength(100);
            entity.Property(a => a.IpAddress).HasMaxLength(50);
            entity.Property(a => a.Remarks).HasMaxLength(500);

            entity.HasOne(a => a.Employee)
                .WithMany(e => e.Attendances)
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // LeaveType
        modelBuilder.Entity<LeaveType>(entity =>
        {
            entity.HasKey(lt => lt.Id);
            entity.Property(lt => lt.Name).HasMaxLength(50).IsRequired();
            entity.HasIndex(lt => lt.Name).IsUnique();
        });

        // LeaveRequest
        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.HasKey(lr => lr.Id);
            entity.Property(lr => lr.Reason).HasMaxLength(500);
            entity.Property(lr => lr.AdminRemarks).HasMaxLength(500);

            entity.HasOne(lr => lr.Employee)
                .WithMany(e => e.LeaveRequests)
                .HasForeignKey(lr => lr.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(lr => lr.LeaveType)
                .WithMany(lt => lt.LeaveRequests)
                .HasForeignKey(lr => lr.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(lr => lr.ApprovedBy)
                .WithMany()
                .HasForeignKey(lr => lr.ApprovedById)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // LeaveBalance
        modelBuilder.Entity<LeaveBalance>(entity =>
        {
            entity.HasKey(lb => lb.Id);
            entity.HasIndex(lb => new { lb.EmployeeId, lb.LeaveTypeId, lb.Year }).IsUnique();
            entity.Ignore(lb => lb.RemainingDays);

            entity.HasOne(lb => lb.Employee)
                .WithMany(e => e.LeaveBalances)
                .HasForeignKey(lb => lb.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(lb => lb.LeaveType)
                .WithMany(lt => lt.LeaveBalances)
                .HasForeignKey(lb => lb.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Holiday
        modelBuilder.Entity<Holiday>(entity =>
        {
            entity.HasKey(h => h.Id);
            entity.HasIndex(h => h.Date);
            entity.Property(h => h.Name).HasMaxLength(100).IsRequired();
        });

        // AuditLog
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.EmployeeId);
            entity.HasIndex(a => a.Timestamp);
            entity.Property(a => a.TableName).HasMaxLength(100).IsRequired();
            entity.Property(a => a.FieldName).HasMaxLength(100).IsRequired();
            entity.Property(a => a.PreviousValue).HasMaxLength(500);
            entity.Property(a => a.NewValue).HasMaxLength(500);
            entity.Property(a => a.AdminName).HasMaxLength(200).IsRequired();
            entity.Property(a => a.Reason).HasMaxLength(500);

            entity.HasOne(a => a.Employee)
                .WithMany()
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Notification
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.HasIndex(n => new { n.EmployeeId, n.IsRead });
            entity.Property(n => n.Title).HasMaxLength(200).IsRequired();
            entity.Property(n => n.Message).HasMaxLength(1000).IsRequired();

            entity.HasOne(n => n.Employee)
                .WithMany(e => e.Notifications)
                .HasForeignKey(n => n.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AppSetting
        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => s.Key).IsUnique();
            entity.Property(s => s.Key).HasMaxLength(100).IsRequired();
            entity.Property(s => s.Value).HasMaxLength(500).IsRequired();
        });

        // IdleLog
        modelBuilder.Entity<IdleLog>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.HasIndex(i => new { i.EmployeeId, i.Date });

            entity.HasOne(i => i.Employee)
                .WithMany()
                .HasForeignKey(i => i.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Department>().HasData(
            new Department { Id = 1, Name = "Accounting", Description = "Accounting Department", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Department { Id = 2, Name = "Tax", Description = "Tax Department", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Department { Id = 3, Name = "Audit", Description = "Audit Department", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Department { Id = 4, Name = "Administration", Description = "Administration Department", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );

        modelBuilder.Entity<LeaveType>().HasData(
            new LeaveType { Id = 1, Name = "Casual Leave", DefaultDaysPerYear = 12, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveType { Id = 2, Name = "Sick Leave", DefaultDaysPerYear = 12, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveType { Id = 3, Name = "Earned Leave", DefaultDaysPerYear = 15, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveType { Id = 4, Name = "Compensatory Off", DefaultDaysPerYear = 0, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );

        modelBuilder.Entity<AppSetting>().HasData(
            new AppSetting { Id = 1, Key = "WorkStartTime", Value = "09:30", Description = "Standard work start time (HH:mm)", UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new AppSetting { Id = 2, Key = "WorkEndTime", Value = "18:30", Description = "Standard work end time (HH:mm)", UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new AppSetting { Id = 3, Key = "StandardWorkHours", Value = "9", Description = "Standard working hours per day", UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new AppSetting { Id = 4, Key = "IdleThresholdMinutes", Value = "6", Description = "Minutes of inactivity before marking as idle", UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new AppSetting { Id = 5, Key = "LateThresholdMinutes", Value = "15", Description = "Grace period in minutes for late arrivals", UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new AppSetting { Id = 6, Key = "HalfDayThresholdHours", Value = "4.5", Description = "Minimum hours for half-day attendance", UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new AppSetting { Id = 7, Key = "WeeklyOffDays", Value = "Saturday,Sunday", Description = "Weekly off days", UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
