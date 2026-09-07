using CommandLine;

namespace AssimilationSoftware.Buildster.CLI.Options;

[Verb("del-package", HelpText = "Remove a specific package from a configured project")]
public class DeletePackageOptions
{
    [Option('p', "project", HelpText = "The project where the package is found", Required = true)]
    public string ProjectName { get; set; }

    [Option('i', "id", HelpText = "The ID of the package to remove", Group = "package-id")]
    public int PackageId { get; set; }

    [Option('s', "source", HelpText = "The source folder of the package to remove", Group = "package-id")]
    public string PackageSource { get; set; }
}