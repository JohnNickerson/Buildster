using System.Reflection;
using AssimilationSoftware.Buildster.CLI.Controllers;
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
        Parser.Default.ParseArguments(args, verbTypes)
            .WithParsed<AddBuildOptions>(opts => new BuildsController().Add(opts))
            .WithParsed<AddMachineOptions>(opts => new MachinesController().Add(opts))
            .WithParsed<AddPackageOptions>(opts => new PackagesController().Add(opts))
            .WithParsed<AddProjectOptions>(opts => new ProjectsController().Add(opts))
            .WithParsed<DeleteBuildOptions>(opts => new BuildsController().Delete(opts))
            .WithParsed<DeleteMachineOptions>(opts => new MachinesController().Delete(opts))
            .WithParsed<DeletePackageOptions>(opts => new PackagesController().Delete(opts))
            .WithParsed<DeleteProjectOptions>(opts => new ProjectsController().Delete(opts))
            .WithParsed<ListBuildsOptions>(opts => new BuildsController().List(opts))
            .WithParsed<ListEnvironmentPathsOptions>(opts => new EnvironmentsController().List(opts))
            .WithParsed<ListMachinesOptions>(opts => new MachinesController().List(opts))
            .WithParsed<ListPackagesOptions>(opts => new PackagesController().List(opts))
            .WithParsed<ListProjectsOptions>(opts => new ProjectsController().List(opts))
            .WithParsed<SetCopyrightOptions>(opts => new ProjectsController().SetCopyright(opts))
            .WithParsed<SetEnvironmentPathOptions>(opts => new EnvironmentsController().SetPath(opts))
            .WithParsed<UpdateBuildOptions>(opts => new BuildsController().Update(opts))
            .WithParsed<UpdateMachineOptions>(opts => new MachinesController().Update(opts))
            .WithParsed<UpdateProjectOptions>(opts => new ProjectsController().Update(opts))
            .WithNotParsed(errs => HandleErrors(errs));
        return 0;
    }

    public static int HandleErrors(object errors)
    {
        return 1;
    }
}