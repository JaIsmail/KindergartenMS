namespace Kindergarten.Core.DTOs;

public class CreateEmployeeDto
{
    public string UserId   { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Phone    { get; set; } = string.Empty;
}

public class EmployeeResponseDto
{
    public int    Id       { get; set; }
    public string UserId   { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email    { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Phone    { get; set; } = string.Empty;
}

public class AttendanceResponseDto
{
    public int       Id            { get; set; }
    public int       EmployeeId    { get; set; }
    public string    EmployeeName  { get; set; } = string.Empty;
    public DateTime  Date          { get; set; }
    public DateTime? CheckInTime   { get; set; }
    public DateTime? CheckOutTime  { get; set; }
    public string    Status        { get; set; } = string.Empty;
    public string?   WorkingHours  { get; set; }
}

public class CheckInDto
{
    public bool BiometricVerified { get; set; } = true;
}
