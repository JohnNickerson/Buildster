namespace AssimilationSoftware.Buildster.Core.Model;

public class Build
{
    public int BuildId { get; set; }
    
    public required string Version { get; set; }

    public string? Notes { get; set; }
    
    public required DateTime Timestamp { get; set; }
    
    public int? EnvironmentId { get; set; }
    public Environment? Environment { get; set; }

    public int ProjectId { get; set; }
    public Project Project { get; set; }
}