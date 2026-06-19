namespace AssimilationSoftware.Buildster.Core.Model;

public class Machine
{
    public int MachineId { get; set; }
    
    public required string Name { get; set; }

    public string? Description { get; set; }
}