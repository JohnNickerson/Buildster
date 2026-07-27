using CommandLine;

namespace AssimilationSoftware.Buildster.CLI.Options;

[Verb("list-env-paths", HelpText = "Show environment paths for a project, machine, or all")]
public class ListEnvironmentPathsOptions
{
    [Option('p', "project", HelpText = "The project whose paths to list")]
    public string ProjectName { get; set; }
    
    [Option('m', "machine", HelpText = "The machine whose paths to list")]
    public string MachineName { get; set; }
}