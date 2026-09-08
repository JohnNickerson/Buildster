using CommandLine;

namespace AssimilationSoftware.Buildster.CLI.Options;

[Verb("list-builds", HelpText = "Show a list of current builds")]
public class ListBuildsOptions
{
        [Option('p', "project", HelpText = "The name of the project to list builds for", Required = false)]
        public string ProjectName { get; set; }

        [Option('b', "bare", HelpText = "Show only version numbers (bare mode)", Required = false)]
        public bool Bare { get; set; } = false;

        [Option("pending", HelpText = "Show only builds that are pending creation, based on Git history", Required = false)]
        public bool Pending { get; set; } = false;
}