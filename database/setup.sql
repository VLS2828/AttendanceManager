-- AttendanceManager Database Setup Script
-- Run this script in SQL Server Management Studio to create the database
-- The application uses EF Core Code-First, so tables are created automatically
-- This script is provided as a reference and for manual setup if needed

-- Create Database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'AttendanceManagerDb')
BEGIN
    CREATE DATABASE AttendanceManagerDb;
END
GO

USE AttendanceManagerDb;
GO

-- Create Tables (EF Core will handle this, but here for reference)

-- Departments
CREATE TABLE IF NOT EXISTS Departments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(MAX) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Employees
CREATE TABLE IF NOT EXISTS Employees (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeCode NVARCHAR(20) NOT NULL UNIQUE,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(200) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(500) NOT NULL,
    Phone NVARCHAR(20) NULL,
    DepartmentId INT NOT NULL FOREIGN KEY REFERENCES Departments(Id),
    Role INT NOT NULL DEFAULT 0, -- 0=Employee, 1=Admin
    IsActive BIT NOT NULL DEFAULT 1,
    JoiningDate DATETIME2 NOT NULL,
    TerminationDate DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Attendance
CREATE TABLE IF NOT EXISTS Attendances (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NOT NULL FOREIGN KEY REFERENCES Employees(Id) ON DELETE CASCADE,
    Date DATE NOT NULL,
    LoginTime DATETIME2 NULL,
    LogoutTime DATETIME2 NULL,
    ComputerName NVARCHAR(100) NULL,
    WindowsUsername NVARCHAR(100) NULL,
    IpAddress NVARCHAR(50) NULL,
    Status INT NOT NULL DEFAULT 0,
    TotalHours FLOAT NOT NULL DEFAULT 0,
    IdleTimeMinutes FLOAT NOT NULL DEFAULT 0,
    EffectiveHours FLOAT NOT NULL DEFAULT 0,
    IsManualEntry BIT NOT NULL DEFAULT 0,
    Remarks NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_Attendance_Employee_Date UNIQUE (EmployeeId, Date)
);

-- Leave Types
CREATE TABLE IF NOT EXISTS LeaveTypes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(MAX) NULL,
    DefaultDaysPerYear INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Leave Requests
CREATE TABLE IF NOT EXISTS LeaveRequests (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NOT NULL FOREIGN KEY REFERENCES Employees(Id) ON DELETE CASCADE,
    LeaveTypeId INT NOT NULL FOREIGN KEY REFERENCES LeaveTypes(Id),
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    TotalDays INT NOT NULL,
    Reason NVARCHAR(500) NULL,
    Status INT NOT NULL DEFAULT 0, -- 0=Pending, 1=Approved, 2=Rejected, 3=Cancelled
    ApprovedById INT NULL FOREIGN KEY REFERENCES Employees(Id),
    ApprovedAt DATETIME2 NULL,
    AdminRemarks NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Leave Balances
CREATE TABLE IF NOT EXISTS LeaveBalances (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NOT NULL FOREIGN KEY REFERENCES Employees(Id) ON DELETE CASCADE,
    LeaveTypeId INT NOT NULL FOREIGN KEY REFERENCES LeaveTypes(Id),
    Year INT NOT NULL,
    TotalDays INT NOT NULL DEFAULT 0,
    UsedDays INT NOT NULL DEFAULT 0,
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_LeaveBalance UNIQUE (EmployeeId, LeaveTypeId, Year)
);

-- Holidays
CREATE TABLE IF NOT EXISTS Holidays (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Date DATE NOT NULL,
    Description NVARCHAR(MAX) NULL,
    IsOptional BIT NOT NULL DEFAULT 0,
    Year INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Audit Logs
CREATE TABLE IF NOT EXISTS AuditLogs (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NOT NULL FOREIGN KEY REFERENCES Employees(Id) ON DELETE CASCADE,
    Date DATE NOT NULL,
    TableName NVARCHAR(100) NOT NULL,
    RecordId BIGINT NOT NULL,
    FieldName NVARCHAR(100) NOT NULL,
    PreviousValue NVARCHAR(500) NULL,
    NewValue NVARCHAR(500) NULL,
    AdminId INT NOT NULL,
    AdminName NVARCHAR(200) NOT NULL,
    Reason NVARCHAR(500) NULL,
    Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Notifications
CREATE TABLE IF NOT EXISTS Notifications (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NOT NULL FOREIGN KEY REFERENCES Employees(Id) ON DELETE CASCADE,
    Title NVARCHAR(200) NOT NULL,
    Message NVARCHAR(1000) NOT NULL,
    Type INT NOT NULL DEFAULT 0,
    IsRead BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- App Settings
CREATE TABLE IF NOT EXISTS AppSettings (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    [Key] NVARCHAR(100) NOT NULL UNIQUE,
    Value NVARCHAR(500) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Idle Logs
CREATE TABLE IF NOT EXISTS IdleLogs (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NOT NULL FOREIGN KEY REFERENCES Employees(Id) ON DELETE CASCADE,
    Date DATE NOT NULL,
    IdleStartTime DATETIME2 NOT NULL,
    IdleEndTime DATETIME2 NOT NULL,
    DurationMinutes FLOAT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Create Indexes
CREATE INDEX IX_Attendance_Date ON Attendances(Date);
CREATE INDEX IX_Attendance_EmployeeId ON Attendances(EmployeeId);
CREATE INDEX IX_AuditLog_EmployeeId ON AuditLogs(EmployeeId);
CREATE INDEX IX_AuditLog_Timestamp ON AuditLogs(Timestamp);
CREATE INDEX IX_Notification_Employee_Read ON Notifications(EmployeeId, IsRead);
CREATE INDEX IX_IdleLog_Employee_Date ON IdleLogs(EmployeeId, Date);
CREATE INDEX IX_Holiday_Date ON Holidays(Date);

-- Seed Data
INSERT INTO Departments (Name, Description) VALUES
    ('Accounting', 'Accounting Department'),
    ('Tax', 'Tax Department'),
    ('Audit', 'Audit Department'),
    ('Administration', 'Administration Department');

INSERT INTO LeaveTypes (Name, DefaultDaysPerYear) VALUES
    ('Casual Leave', 12),
    ('Sick Leave', 12),
    ('Earned Leave', 15),
    ('Compensatory Off', 0);

INSERT INTO AppSettings ([Key], Value, Description) VALUES
    ('WorkStartTime', '09:30', 'Standard work start time (HH:mm)'),
    ('WorkEndTime', '18:30', 'Standard work end time (HH:mm)'),
    ('StandardWorkHours', '9', 'Standard working hours per day'),
    ('IdleThresholdMinutes', '6', 'Minutes of inactivity before marking as idle'),
    ('LateThresholdMinutes', '15', 'Grace period in minutes for late arrivals'),
    ('HalfDayThresholdHours', '4.5', 'Minimum hours for half-day attendance'),
    ('WeeklyOffDays', 'Saturday,Sunday', 'Weekly off days');

PRINT 'Database setup completed successfully.';
GO
