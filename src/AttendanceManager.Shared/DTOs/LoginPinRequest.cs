namespace AttendanceManager.Shared.DTOs;

public class LoginPinRequest
{
    public string Email { get; set; } = string.Empty;
    public string Pin { get; set; } = string.Empty;
}
