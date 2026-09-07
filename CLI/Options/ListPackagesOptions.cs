using CommandLine;

namespace AssimilationSoftware.Buildster.CLI.Options;

[Verb("list-packages", HelpText = "Show the configured package data")]
public class ListPackagesOptions
{
    [Option('p', "project", HelpText = "The project whose packages to list (omit to list for all projects)")]
    public string ProjectName { get; set; }

}