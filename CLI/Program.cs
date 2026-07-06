using System.Reflection;
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
            (AddMachineOptions opts) => AddMachine(opts),
            (AddProjectOptions opts) => AddProject(opts),
            (DeleteBuildOptions opts) => DeleteBuild(opts),
            (DeleteMachineOptions opts) => DeleteMachine(opts),
            (DeleteProjectOptions opts) => DeleteProject(opts),
            (ListBuildsOptions opts) => ListBuilds(opts),
            (ListMachinesOptions opts) => ListMachines(opts),
            (ListProjectsOptions opts) => ListProjects(opts),
            (UpdateBuildOptions opts) => UpdateBuild(opts),
            (UpdateMachineOptions opts) => UpdateMachine(opts),
            (UpdateProjectOptions opts) => UpdateProject(opts),
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

    public static int AddMachine(AddMachineOptions opts)
    {
        using (var context = new BuildsContext())
        {
            var machine = new Machine()
            {
                Name = opts.Name,
                Description = opts.Description
            };
            context.Machines.Add(machine);
            context.SaveChanges();
            ListMachines();
        }
        return 0;
    }

    public static int AddProject(AddProjectOptions opts)
    {
        using (var context = new BuildsContext())
        {
            var project = new Project()
            {
                Name = opts.Name,
                Description = opts.Description
            };
            context.Projects.Add(project);
            if (opts.SourceFolder is not null)
            {
                // Add the current computer, if required.
                var currentMachine = context.FindMachine(System.Environment.MachineName);
                if (currentMachine is null)
                {
                    currentMachine = new Machine() { Name = System.Environment.MachineName };
                    context.Machines.Add(currentMachine);
                }
                var projectPath = new ProjectPath()
                {
                    Path = opts.SourceFolder,
                    Machine = currentMachine,
                    Project = project
                };
                context.ProjectPaths.Add(projectPath);
            }
            context.SaveChanges();
            ListProjects();
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

    public static int DeleteMachine(DeleteMachineOptions opts)
    {
        using (var context = new BuildsContext())
        {
            var machine = context.Machines.FirstOrDefault(m => m.Name.ToLower() == opts.Name.ToLower());
            if (machine is null)
            {
                Console.WriteLine($"Machine {opts.Name} not found");
                return 0;
            }
            context.Machines.Remove(machine);
            // Remove related data, too.
            context.EnvironmentPaths.RemoveRange(context.EnvironmentPaths.Where(ep => ep.MachineId == machine.MachineId));
            context.ProjectPaths.RemoveRange(context.ProjectPaths.Where(pp => pp.MachineId == machine.MachineId));
            ListMachines();
        }
        return 0;
    }

    public static int DeleteProject(DeleteProjectOptions opts)
    {
        using (var context = new BuildsContext())
        {
            var project = context.FindProject(opts.Name);
            if (project is null)
            {
                Console.WriteLine($"Cannot find project {opts.Name}");
                return 0;
            }
            context.Projects.Remove(project);
            context.Builds.RemoveRange(context.Builds.Where(b => b.ProjectId == project.ProjectId));
            context.ProjectPaths.RemoveRange(context.ProjectPaths.Where(pp => pp.ProjectId == project.ProjectId));
            context.Packages.RemoveRange(context.Packages.Where(p => p.ProjectId == project.ProjectId));
            context.EnvironmentPaths.RemoveRange(context.EnvironmentPaths.Where(ep => ep.ProjectId == project.ProjectId));
            context.SaveChanges();
            ListProjects();
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

    public static int ListMachines(ListMachinesOptions? opts = null)
    {
        using (var context = new BuildsContext())
        {
            var table = new Table();
            table.AddColumns("Machine", "Description");
            foreach (var machine in context.Machines)
            {
                table.AddRow(machine.Name, machine.Description ?? string.Empty);
            }
            AnsiConsole.Write(table);
        }
        return 0;
    }

    public static int ListProjects(ListProjectsOptions? opts = null)
    {
        using (var context = new BuildsContext())
        {
            // TODO: Display only one line per project, remove the "Machine" column, and display the path for the current machine only.
            Table table = new Table();
            table.AddColumns("Project", "Description", "Machine", "Path");
            foreach (var proj in context.Projects)
            {
                if (opts?.Verbose ?? false)
                {
                    bool row1 = true;
                    foreach (var path in context.ProjectPaths.Include(pp => pp.Machine).Where(pp => pp.ProjectId == proj.ProjectId))
                    {
                        if (row1)
                        {
                            table.AddRow(proj.Name, proj.Description ?? string.Empty, path.Machine?.Name ?? string.Empty, path.Path);
                            row1 = false;
                        }
                        else
                        {
                            table.AddRow(string.Empty, string.Empty, path.Machine?.Name ?? string.Empty, path.Path);
                        }
                    }
                }
                else
                {
                    var path = context.FindProjectPath(proj, System.Environment.MachineName);
                    Console.WriteLine($"{proj.Name} @ {path?.Path ?? "(no path found)"}");
                }
            }
            if (opts?.Verbose ?? false)
            {
                AnsiConsole.Write(table);
            }

        }
        return 0;
    }

    public static int UpdateMachine(UpdateMachineOptions opts)
    {
        // Find the machine.
        using (var context = new BuildsContext())
        {
            var machine = context.FindMachine(opts.OriginalName);
            if (machine is null)
            {
                Console.WriteLine($"Machine not found: {opts.OriginalName}");
                return 0;
            }
            if (!string.IsNullOrWhiteSpace(opts.UpdatedName))
            {
                machine.Name = opts.UpdatedName;
            }
            if (!string.IsNullOrWhiteSpace(opts.UpdatedDescription))
            {
                machine.Description = opts.UpdatedDescription;
            }
            context.SaveChanges();
            ListMachines();
        }
        return 0;
    }

    public static int UpdateProject(UpdateProjectOptions opts)
    {
        using (var context = new BuildsContext())
        {
            var project = context.FindProject(opts.SearchName);
            if (project is null)
            {
                Console.WriteLine($"Project not found: {opts.SearchName}");
                return 0;
            }
            if (!string.IsNullOrEmpty(opts.UpdatedName))
            {
                project.Name = opts.UpdatedName;
            }
            if (!string.IsNullOrEmpty(opts.UpdatedDescription))
            {
                project.Description = opts.UpdatedDescription;
            }
            if (!string.IsNullOrEmpty(opts.UpdatedSourcePath))
            {
                var machine = context.FindMachine(opts.SourcePathMachine);
                if (machine is null)
                {
                    Console.WriteLine($"Could not find target machine: {opts.SourcePathMachine}");
                    return 0;
                }
                // Update or add source path.
                context.UpdateProjectPath(project, machine, opts.UpdatedSourcePath);
            }
            context.SaveChanges();
            ListProjects();
        }
        return 0;
    }
}