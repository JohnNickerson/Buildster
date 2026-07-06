using CommandLine;
namespace AssimilationSoftware.Buildster.CLI.Options;

[Verb("promote", HelpText = "Promote a build or builds to the next environment")]
public class UpdateBuildOptions
{
        [Option('p', "project", HelpText = "The name of the project containing the build to reject.", Required = true)]
        public string ProjectName { get; set; }

        [Option('e', "env", HelpText = "The environment where the build currently resides.")]
        public string Environment { get; set; }
}