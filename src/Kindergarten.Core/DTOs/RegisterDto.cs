namespace Kindergarten.Core.DTOs;

public class RegisterDto
{
    public string FullName    { get; set; } = string.Empty;
    public string Email       { get; set; } = string.Empty;
    public string Password    { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string RoleType    { get; set; } = string.Empty; // Parent, Driver, Admin, Employee
    public int    TenantId   { get; set; } = 1;
}

public class CreateTenantAdminDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email    { get; set; } = string.Empty;
    public string Password { get; set; } = "Admin@123456";
}
