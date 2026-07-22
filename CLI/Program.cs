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
            (AddBuildOptions opts) => BuildsController.Add(opts),
            (AddMachineOptions opts) => MachinesController.Add(opts),
            (AddProjectOptions opts) => ProjectsController.Add(opts),
            (DeleteBuildOptions opts) => BuildsController.Delete(opts),
            (DeleteMachineOptions opts) => MachinesController.Delete(opts),
            (DeleteProjectOptions opts) => ProjectsController.Delete(opts),
            (ListBuildsOptions opts) => BuildsController.List(opts),
            (ListMachinesOptions opts) => MachinesController.List(opts),
            (ListProjectsOptions opts) => ProjectsController.List(opts),
            (UpdateBuildOptions opts) => BuildsController.Update(opts),
            (UpdateMachineOptions opts) => MachinesController.Update(opts),
            (UpdateProjectOptions opts) => ProjectsController.Update(opts),
            errs => 1);
    }
}