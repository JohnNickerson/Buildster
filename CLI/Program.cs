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
            (AddBuildOptions opts) => AddBuild(opts),
            (AddMachineOptions opts) => MachinesController.Add(opts),
            (AddProjectOptions opts) => ProjectsController.Add(opts),
            (DeleteBuildOptions opts) => DeleteBuild(opts),
            (DeleteMachineOptions opts) => MachinesController.Delete(opts),
            (DeleteProjectOptions opts) => ProjectsController.Delete(opts),
            (ListBuildsOptions opts) => ListBuilds(opts),
            (ListMachinesOptions opts) => MachinesController.List(opts),
            (ListProjectsOptions opts) => ProjectsController.List(opts),
            (UpdateBuildOptions opts) => UpdateBuild(opts),
            (UpdateMachineOptions opts) => MachinesController.Update(opts),
            (UpdateProjectOptions opts) => ProjectsController.Update(opts),
            errs => 1);
    }

    private static int UpdateBuild(UpdateBuildOptions opts)
    {
        using (var buildRepo = new BuildsContext())
        {
            // Check that the build exists.
            var build = buildRepo.FindDeployedBuild(opts.ProjectName, opts.Environment);
            if (build == null)
            {
                Console.WriteLine($"No build found in '{opts.Environment}' for project '{opts.ProjectName}'.");
                return 1;
            }
            // 1. Get the next environment.
            // TODO: Set up environment order in the database and use that to determine the next environment. For now, just hardcode it.
            string? nextEnvironment = GetNextEnvironment(opts.Environment);
            if (string.IsNullOrEmpty(nextEnvironment))
            {
                Console.WriteLine($"No next environment found after '{opts.Environment}'.");
                return 1;
            }
            // 2. Reject any existing build in the next environment.
            var existingBuild = buildRepo.FindDeployedBuild(opts.ProjectName, nextEnvironment);
            if (existingBuild != null)
            {
                buildRepo.Builds.Remove(existingBuild);
                Console.WriteLine($"Existing build '{existingBuild.Version}' for project '{opts.ProjectName}' in environment '{nextEnvironment}' rejected.");
            }
            // 3. Promote the build to the next environment.
            var environment = buildRepo.FindEnvironment(nextEnvironment);
            if (environment == null)
            {
                Console.WriteLine($"Could not find environment '{nextEnvironment}'.");
                return 1;
            }
            build.EnvironmentId = environment?.EnvironmentId;
            buildRepo.Update(build);
            buildRepo.SaveChanges();
            Console.WriteLine($"Build '{build.Version}' for project '{opts.ProjectName}' promoted to '{nextEnvironment}'.");
            ListBuilds(new ListBuildsOptions { ProjectName = opts.ProjectName });
        }
        return 0;
    }

    private static string? GetNextEnvironment(string environment)
    {
        switch (environment.ToLower())
        {
            case "integration":
                return "Testing";
            case "testing":
                return "Production";
            default:
                return null;
        }
    }

    public static int AddBuild(AddBuildOptions opts)
    {
        using (var context = new BuildsContext())
        {
            // Get the project by name.
            var project = context.FindProject(opts.ProjectName);
            if (project is null)
            {
                Console.WriteLine($"Cannot find a project with the name {opts.ProjectName}");
                return 0;
            }
            var integration = context.FindEnvironment(Integration)!;
            var build = new Build()
            {
                Timestamp = DateTime.Now,
                Version = opts.Version,
                Environment = integration,
                Notes = opts.Description,
                Project = project
            };
            // Reject any build currently in the environment.
            context.Builds.RemoveRange(context.Builds.Where(b => b.ProjectId == project.ProjectId && b.EnvironmentId == integration.EnvironmentId));
            // TODO: Add tag to source control, push tag to origin if present, update version numbers, update copyright year if needed, add release notes, build packages
            // This will all be ported functionality from Buildster 0.5
            context.Builds.Add(build);
            context.SaveChanges();
            ListBuilds();
        }
        return 0;
    }

    public static int DeleteBuild(DeleteBuildOptions opts)
    {
        using (var context = new BuildsContext())
        {
            var build = context.Builds
                .Include(b => b.Project)
                .Include(b => b.Environment)
                .FirstOrDefault(b =>
                    b.Project.Name.ToLower() == opts.ProjectName.ToLower() &&
                    b.Environment != null &&
                    b.Environment.Name.ToLower() == opts.EnvironmentName.ToLower()
                );
            if (build is null)
            {
                Console.WriteLine($"Build not found in {opts.EnvironmentName} for {opts.ProjectName}");
                return 0;
            }
            // TODO: Perhaps mark a build as rejected once we have build history in place. Will require a new property.
            context.Builds.Remove(build);
            context.SaveChanges();
            Console.WriteLine($"Build {build.Version} removed from {build.Environment.Name} for {build.Project.Name}");
        }
        return 0;
    }

    public static int ListBuilds(ListBuildsOptions? opts = null)
    {
        using (var context = new BuildsContext())
        {
            var searchProjectName = opts?.ProjectName?.ToLower();
            List<Build> builds = context.Builds
                .Include(b => b.Project)
                .Include(b => b.Environment)
                .Where(b => searchProjectName == null || b.Project.Name.ToLower() == searchProjectName)
                .ToList();

            var table = new Table();
            table.AddColumns("Project", "Integration", "Testing", "Production");
            foreach (var project in builds.Select(b => b.Project.Name).Distinct().OrderBy(p => p))
            {
                var integrationBuild = builds.FirstOrDefault(b => b.Project.Name == project && b.Environment?.Name == Integration);
                var testingBuild = builds.FirstOrDefault(b => b.Project.Name == project && b.Environment?.Name == "Testing");
                var productionBuild = builds.FirstOrDefault(b => b.Project.Name == project && b.Environment?.Name == "Production");
                if (opts.Bare)
                {
                    table.AddRow(
                        project,
                        integrationBuild?.Version ?? string.Empty,
                        testingBuild?.Version ?? string.Empty,
                        productionBuild?.Version ?? string.Empty
                    );
                }
                else
                {
                    var intPanel = DisplayPanel(integrationBuild, opts.Bare);
                    var testPanel = DisplayPanel(testingBuild, opts.Bare);
                    var prodPanel = DisplayPanel(productionBuild, opts.Bare);
                    table.AddRow(
                        new Markup(project),
                        intPanel,
                        testPanel,
                        prodPanel
                    );
                }
            }
            AnsiConsole.Write(table);
        }
        return 0;
    }

    private static Panel DisplayPanel(Build? build, bool bare)
    {
        if (build == null)
        {
            return new Panel("-").NoBorder();
        }
        if (bare)
        {
            return new Panel(build.Version.ToString());
        }
        return new Panel($"Version: {build.Version}\nDate: {build.Timestamp:yyyy-MM-dd}\nNotes: {build.Notes}");
    }
}