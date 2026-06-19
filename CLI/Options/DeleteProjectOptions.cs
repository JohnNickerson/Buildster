using CommandLine;
namespace AssimilationSoftware.Buildster.CLI.Options;

[Verb("del-project", HelpText = "Delete a project and all its associated data")]
public class DeleteProjectOptions
{
    [Option('p', "project", HelpText = "The name of the project to delete", Required = true)]
    public string Name{ get; set; }
}