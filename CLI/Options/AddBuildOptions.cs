using CommandLine;
namespace AssimilationSoftware.Buildster.CLI.Options;

[Verb("new-build", HelpText = "Create a new build for a project")]
public class AddBuildOptions
{
    [Option('p', "project", HelpText = "The project for the new build", Required = true)]
    public required string ProjectName { get; set; }

    [Option('n', "desc", HelpText = "A short description of changes made")]
    public string? Description { get; set; }

    [Option('v', "version", HelpText = "The new version number", Required = true)]
    public required string Version { get; set; }
}