namespace Kindergarten.Core.DTOs;

public class AuthResponseDto
{
    public string Token   { get; set; } = string.Empty;
    public string UserId  { get; set; } = string.Empty;
    public string Email   { get; set; } = string.Empty;
    public string Role    { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime Expiry   { get; set; }
    public int     TenantId { get; set; }
}
