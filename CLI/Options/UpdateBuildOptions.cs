using CommandLine;
namespace AssimilationSoftware.Buildster.CLI.Options;

[Verb("promote", HelpText = "Promote a build or builds to the next environment")]
public class UpdateBuildOptions
{
        [Option('p', "project", HelpText = "The name of the project containing the build to promote.", Required = true)]
        public string ProjectName { get; set; }

        [Option('e', "env", HelpText = "The environment where the build currently resides.")]
        public string Environment { get; set; }

        [Option("data-only", HelpText = "Only record data about the build, don't take any file actions")]
        public bool DataOnly { get; set; } = false;
}