using CommandLine;
namespace AssimilationSoftware.Buildster.CLI.Options;

[Verb("add-project", HelpText = "Add a new project")]
public class AddProjectOptions
{
    [Option('n', "name", HelpText = "Project name", Required = true)]
    public required string Name { get; set; }

    [Option('d', "desc", HelpText = "A short description of the project")]
    public string? Description { get; set; }
    
    [Option('s', "source", HelpText = "The folder on this machine where the project's source code is located, if any")]
    public string? SourceFolder { get; set; }
}