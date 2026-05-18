namespace Kindergarten.Core.DTOs;

public class CreateChildDto
{
    public string Name        { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public string Class       { get; set; } = string.Empty;
    public string HealthNotes { get; set; } = string.Empty;
}

public class ChildResponseDto
{
    public int    Id           { get; set; }
    public string Name         { get; set; } = string.Empty;
    public DateTime BirthDate  { get; set; }
    public string Class        { get; set; } = string.Empty;
    public string HealthNotes  { get; set; } = string.Empty;
    public string ParentId     { get; set; } = string.Empty;
    public string ParentName   { get; set; } = string.Empty;
}
