using CommandLine;
namespace  AssimilationSoftware.Buildster.CLI.Options;

[Verb("del-machine", HelpText = "Deletes a machine from the database")]
public class DeleteMachineOptions
{
    [Option('n', "name", HelpText = "The name of the machine to delete", Required = true)]
    public string Name{ get; set; }
}