namespace AssimilationSoftware.Buildster.Core.Model;

public class Project
{
    public int ProjectId { get; set; }
    
    public required string Name { get; set; }
    
    public string? Description { get; set; }
}
