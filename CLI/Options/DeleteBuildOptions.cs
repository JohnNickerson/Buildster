using CommandLine;
namespace AssimilationSoftware.Buildster.CLI.Options;

[Verb("reject", HelpText = "Reject a build and remove it from the database")]
public class DeleteBuildOptions
{
    [Option('p', "project", HelpText = "The project that owns the build", Required = true)]
    public string ProjectName { get; set; }
    
    [Option('e', "env", HelpText = "The current environment where the build exists", Required = true)]
    public string EnvironmentName{ get; set; }
}