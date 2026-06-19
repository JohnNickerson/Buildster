using CommandLine;
namespace AssimilationSoftware.Buildster.CLI.Options;

[Verb("update-machine", HelpText = "Update the name or description of a machine")]
public class UpdateMachineOptions
{
    [Option('n', "name", HelpText = "The original name to search for", Required = true)]
    public required string OriginalName { get; set; }

    [Option("new-name", HelpText = "The new name to use for this machine")]
    public string? UpdatedName { get; set; }

    [Option("desc", HelpText = "An updated description for this machine")]
    public string? UpdatedDescription { get; set; }
}