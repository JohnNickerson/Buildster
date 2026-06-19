using CommandLine;

namespace AssimilationSoftware.Buildster.CLI.Options;

[Verb("add-machine", HelpText = "Add a new host machine for path localisation")]
public class AddMachineOptions
{
    [Option('n', "name", HelpText = "The name of the machine to add. Defaults to current machine name.")]
    public required string Name { get; set; } = Environment.MachineName;

    [Option('d', "desc", HelpText = "Optional description")]
    public string? Description { get; set; }
}