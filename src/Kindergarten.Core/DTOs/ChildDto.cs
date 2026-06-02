namespace Kindergarten.Core.DTOs;

public class CreateChildDto
{
    public string  Name         { get; set; } = string.Empty;
    public string  NationalId   { get; set; } = string.Empty;
    public DateTime BirthDate   { get; set; }
    public string  Class        { get; set; } = string.Empty;
    public string  AgeGroup     { get; set; } = string.Empty;
    public string  MotherPhone  { get; set; } = string.Empty;
    public string  Neighborhood { get; set; } = string.Empty;
    public string  HealthNotes  { get; set; } = string.Empty;
    public bool    IsActive     { get; set; } = true;
    public string? ParentId     { get; set; }
}

public class ChildResponseDto
{
    public int      Id           { get; set; }
    public string   Name         { get; set; } = string.Empty;
    public string   NationalId   { get; set; } = string.Empty;
    public DateTime BirthDate    { get; set; }
    public string   Class        { get; set; } = string.Empty;
    public string   AgeGroup     { get; set; } = string.Empty;
    public string   MotherPhone  { get; set; } = string.Empty;
    public string   Neighborhood { get; set; } = string.Empty;
    public string   HealthNotes  { get; set; } = string.Empty;
    public bool     IsActive     { get; set; }
    public string   ParentId     { get; set; } = string.Empty;
    public string   ParentName   { get; set; } = string.Empty;
}
