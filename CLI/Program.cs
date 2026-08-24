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
        return Parser.Default.ParseArguments(args, verbTypes)
        .MapResult(
            (AddBuildOptions opts) => new BuildsController().Add(opts),
            (AddMachineOptions opts) => new MachinesController().Add(opts),
            (AddProjectOptions opts) => new ProjectsController().Add(opts),
            (DeleteBuildOptions opts) => new BuildsController().Delete(opts),
            (DeleteMachineOptions opts) => new MachinesController().Delete(opts),
            (DeleteProjectOptions opts) => new ProjectsController().Delete(opts),
            (ListBuildsOptions opts) => new BuildsController().List(opts),
            (ListEnvironmentPathsOptions opts) => new EnvironmentsController().List(opts),
            (ListMachinesOptions opts) => new MachinesController().List(opts),
            (ListProjectsOptions opts) => new ProjectsController().List(opts),
            (SetCopyrightOptions opts) => new ProjectsController().SetCopyright(opts),
            (SetEnvironmentPathOptions opts) => new EnvironmentsController().SetPath(opts),
            (UpdateBuildOptions opts) => new BuildsController().Update(opts),
            (UpdateMachineOptions opts) => new MachinesController().Update(opts),
            (UpdateProjectOptions opts) => new ProjectsController().Update(opts),
            errs => 1);
    }
}