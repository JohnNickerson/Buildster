namespace AssimilationSoftware.Buildster.Core.Model;

public class EnvironmentPath
{
    public int EnvironmentPathId { get; set; }
    
    public int EnvironmentId { get; set; }
    public Environment Environment { get; set; }

    public int ProjectId { get; set; }
    public Project Project { get; set; }
    
    public int MachineId { get; set; }
    public Machine Machine { get; set; }
    
    public required string Path { get; set; }
}