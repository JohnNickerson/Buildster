using System.Reflection;
using AssimilationSoftware.Buildster.Core.Controllers;
using AssimilationSoftware.Buildster.CLI.Options;
using AssimilationSoftware.Buildster.Core;
using AssimilationSoftware.Buildster.Core.Model;
using CommandLine;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Spectre.Console;
namespace AssimilationSoftware.Buildster.CLI;

public class Program
{
    private const string Integration = "Integration";

    public static int Main(string[] args)
    {
        using (var context = new BuildsContext())
        {
            try
            {
                // Temporarily mute the EF Core 9 pending model guard during migration
                //context.Database.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));

                // Automatically updates the DB to match your migration files
                context.Database.Migrate();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Database migration failed: {ex.Message}");
                Console.ResetColor();
                return 1; // Exit if DB can't initialize
            }
        }

        Type[] verbTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttribute<VerbAttribute>() != null).ToArray();
        var statusWriter = new ConsoleStatusWriter();
        var tableWriter = new SpectreTableWriter();
        Parser.Default.ParseArguments(args, verbTypes)
            .WithParsed<AddBuildOptions>(opts => new BuildsController(statusWriter, tableWriter).Add(opts.ProjectName, opts.Version, opts.BuildDate, opts.Description, opts.DataOnly))
            .WithParsed<AddMachineOptions>(opts => new MachinesController(statusWriter, tableWriter).Add(opts.Name, opts.Description))
            .WithParsed<AddPackageOptions>(opts => new PackagesController(statusWriter, tableWriter).Add(opts.ProjectName, opts.SourceFolder, opts.DeployFolder, opts.IsNuGet))
            .WithParsed<AddProjectOptions>(opts => new ProjectsController(statusWriter, tableWriter).Add(opts.Name, opts.Description, opts.SourceFolder))
            .WithParsed<DeleteBuildOptions>(opts => new BuildsController(statusWriter, tableWriter).Delete(opts.ProjectName, opts.EnvironmentName, opts.DataOnly))
            .WithParsed<DeleteMachineOptions>(opts => new MachinesController(statusWriter, tableWriter).Delete(opts.Name))
            .WithParsed<DeletePackageOptions>(opts => new PackagesController(statusWriter, tableWriter).Delete(opts.ProjectName, opts.PackageId, opts.PackageSource))
            .WithParsed<DeleteProjectOptions>(opts => new ProjectsController(statusWriter, tableWriter).Delete(opts.Name))
            .WithParsed<ListBuildsOptions>(opts => new BuildsController(statusWriter, tableWriter).List(opts.ProjectName, opts.Bare, opts.Pending))
            .WithParsed<ListEnvironmentPathsOptions>(opts => new EnvironmentsController(statusWriter, tableWriter).List(opts.ProjectName, opts.MachineName))
            .WithParsed<ListMachinesOptions>(_ => new MachinesController(statusWriter, tableWriter).List())
            .WithParsed<ListPackagesOptions>(opts => new PackagesController(statusWriter, tableWriter).List(opts.ProjectName))
            .WithParsed<ListProjectsOptions>(opts => new ProjectsController(statusWriter, tableWriter).List(opts.Verbose))
            .WithParsed<SetCopyrightOptions>(opts => new ProjectsController(statusWriter, tableWriter).SetCopyright(opts.ProjectName, opts.CompanyName, opts.YearString))
            .WithParsed<SetEnvironmentPathOptions>(opts => new EnvironmentsController(statusWriter, tableWriter).SetPath(opts.ProjectName, opts.MachineName, opts.EnvironmentName, opts.Folder))
            .WithParsed<UpdateBuildOptions>(opts => new BuildsController(statusWriter, tableWriter).Update(opts.ProjectName, opts.Environment, opts.DataOnly))
            .WithParsed<UpdateMachineOptions>(opts => new MachinesController(statusWriter, tableWriter).Update(opts.OriginalName, opts.UpdatedName, opts.UpdatedDescription))
            .WithParsed<UpdateProjectOptions>(opts => new ProjectsController(statusWriter, tableWriter).Update(opts.SearchName, opts.UpdatedName, opts.UpdatedDescription, opts.UpdatedSourcePath, opts.SourcePathMachine))
            .WithNotParsed(errs => HandleErrors(errs));
        return 0;
    }

    public static int HandleErrors(object errors)
    {
        return 1;
    }
}