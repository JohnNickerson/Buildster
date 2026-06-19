namespace AssimilationSoftware.Buildster.Core.Model;

/// <summary>
/// The source path for a project on a particular host machine.
/// </summary>
/// <remarks>
/// Because Buildster is designed to run as portable software, the path to a project's source code may vary, depending on where it is running at the time.
/// </remarks>
public class ProjectPath
{
    public int ProjectPathId { get; set; }

    public int ProjectId { get; set; }
    public Project Project { get; set; }

    public int MachineId { get; set; }
    public Machine Machine { get; set; }

    public required string Path { get; set; }
}