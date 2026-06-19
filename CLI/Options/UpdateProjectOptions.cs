using CommandLine;
namespace AssimilationSoftware.Buildster.CLI.Options;

[Verb("update-project", HelpText = "Update a project's details")]
public class UpdateProjectOptions
{
    [Option('p', "project", HelpText = "The current name to search for", Required = true)]
    public required string SearchName { get; set; }

    // Change name or description, but most likely set the current source control path on the current machine.
    [Option("new-name", HelpText = "A new name to use for the project")]
    public string? UpdatedName { get; set; }

    [Option("desc", HelpText = "A new description to use for the project")]
    public string? UpdatedDescription { get; set; }

    [Option("path", HelpText = "New or updated path to the source code")]
    public string? UpdatedSourcePath { get; set; }

    [Option("machine", HelpText = "The host machine to set the source path on, if not current")]
    public string SourcePathMachine { get; set; } = Environment.MachineName;
}