using CommandLine;

namespace AssimilationSoftware.Buildster.CLI.Options;

[Verb("list-projects", HelpText = "Show a list of projects")]
public class ListProjectsOptions
{
    [Option('v', "verbose", HelpText = "Show all the project information")]
    public bool Verbose { get; set; }
}