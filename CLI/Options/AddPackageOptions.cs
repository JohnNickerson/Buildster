using CommandLine;

namespace AssimilationSoftware.Buildster.CLI.Options;

[Verb("add-package", HelpText = "Add info about a package contained within a project")]
public class AddPackageOptions
{
    [Option('p', "project", HelpText = "The name of the project the package belongs to", Required = true)]
    public string ProjectName { get; set; }

    [Option('s', "source", HelpText = "The source folder within the solution")]
    public string SourceFolder { get; set; }

    [Option('d', "deploy", HelpText = "The relative path where the built package should be deployed")]
    public string DeployFolder { get; set; }

    [Option("nuget", HelpText = "True if this package should be built as a NuGet package, false otherwise")]
    public bool IsNuGet {get;set;}
}