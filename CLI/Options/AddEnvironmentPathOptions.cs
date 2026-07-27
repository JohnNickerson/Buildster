using CommandLine;

namespace AssimilationSoftware.Buildster.CLI.Options;

[Verb("add-env-path", HelpText = "Specify an environment path for a project on a machine")]
public class AddEnvironmentPathOptions
{
    [Option('p', "project", HelpText = "The project context for the environment path", Required = true)]
    public string ProjectName { get; set; }
    
    [Option('m', "machine", HelpText = "The machine where the path lives. Defaults to this machine")]
    public string MachineName { get; set; }

    [Option('e', "env", HelpText = "The environment whose path to set", Required = true)]
    public string EnvironmentName{ get; set; }

    [Option('f', "folder", HelpText = "The folder location that represents the given environment for this project on the given machine")]
    public string Folder { get; set; }
}
