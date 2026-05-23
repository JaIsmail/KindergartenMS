namespace Kindergarten.Core.Entities;

public class Tenant
{
    public int      Id        { get; set; }
    public string   NameAr    { get; set; } = string.Empty;
    public string   NameEn    { get; set; } = string.Empty;
    public string?  Logo      { get; set; }
    public string?  City      { get; set; }
    public string?  Address   { get; set; }
    public string?  Phone     { get; set; }
    public string?  Email     { get; set; }
    public string   Plan      { get; set; } = "Basic";
    public bool     IsActive  { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string?  Settings  { get; set; }


}
