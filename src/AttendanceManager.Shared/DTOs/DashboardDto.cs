namespace AttendanceManager.Shared.DTOs;

public class AdminDashboardDto
{
    public int TotalEmployees { get; set; }
    public int EmployeesOnline { get; set; }
    public int EmployeesOffline { get; set; }
    public int PresentToday { get; set; }
    public int AbsentToday { get; set; }
    public int OnLeaveToday { get; set; }
    public int LateArrivals { get; set; }
    public int EarlyDepartures { get; set; }
    public int HolidaysThisMonth { get; set; }
    public List<AttendanceDto> TodayAttendance { get; set; } = new();
}

public class EmployeeDashboardDto
{
    public AttendanceDto? TodayAttendance { get; set; }
    public List<LeaveBalanceDto> LeaveBalances { get; set; } = new();
    public List<AttendanceDto> MonthlyAttendance { get; set; } = new();
    public MonthlySummaryDto MonthlySummary { get; set; } = new();
}

public class LeaveBalanceDto
{
    public string LeaveTypeName { get; set; } = string.Empty;
    public int TotalDays { get; set; }
    public int UsedDays { get; set; }
    public int RemainingDays { get; set; }
}

public class MonthlySummaryDto
{
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int LeaveDays { get; set; }
    public int HalfDays { get; set; }
    public int Holidays { get; set; }
    public int WeeklyOffs { get; set; }
    public double TotalWorkingHours { get; set; }
    public double TotalIdleMinutes { get; set; }
    public double TotalEffectiveHours { get; set; }
    public double AverageWorkingHours { get; set; }
}
