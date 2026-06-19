namespace AssimilationSoftware.Buildster.Core.Model;

public class Package
{
    public int PackageId { get; set; }

    public required string SourceFolder { get; set; }
    
    public required string DeployFolder { get; set; }

    public bool IsNuGet{ get; set; }

    public int ProjectId { get; set; }
    public Project Project { get; set; }
}