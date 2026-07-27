using AssimilationSoftware.Buildster.CLI.Options;
using AssimilationSoftware.Buildster.Core;
using AssimilationSoftware.Buildster.Core.Model;
using AssimilationSoftware.Buildster.Core.Utils;
using Microsoft.EntityFrameworkCore;
using Spectre.Console;

namespace AssimilationSoftware.Buildster.CLI.Controllers;

public class BuildsController
{

    public static int Add(AddBuildOptions opts)
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
            var integration = context.FindEnvironment("Integration")!;
            var build = new Build()
            {
                Timestamp = DateTime.Now,
                Version = opts.Version,
                Environment = integration,
                Notes = opts.Description,
                Project = project
            };
            var path = context.FindProjectPath(project, System.Environment.MachineName);
            if (path is null)
            {
                Console.WriteLine($"Cannot find a path for project {project.Name} on this machine.");
                return 0;
            }
            var version = new VersionNumber(opts.Version);
            var statusWriter = new ConsoleStatusWriter();

            // Reject any build currently in the environment.
            context.Builds.RemoveRange(context.Builds.Where(b => b.ProjectId == project.ProjectId && b.EnvironmentId == integration.EnvironmentId));
            VersionInfo.Update(path.Path, version, statusWriter);
            // Add tag to source control, push tag to origin if present
            VersionInfo.Tag(path.Path, version, statusWriter);
            // Update copyright if needed
            var company = VersionInfo.GetCompany(path.Path, statusWriter).FirstOrDefault() ?? string.Empty;
            VersionInfo.UpdateCopyright(path.Path, company, DateTime.Now.Year, statusWriter);
            // Add release notes
            ReleaseNotes.AppendNotes(path.Path, DateTime.Now, version, opts.Description?.Split(['.'], StringSplitOptions.RemoveEmptyEntries) ?? []);

            // TODO: build the actual packages
            // (first need to store package info in the database)

            context.Builds.Add(build);
            context.SaveChanges();
            List();
        }
        return 0;
    }

    public static int Delete(DeleteBuildOptions opts)
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

    public static int Update(UpdateBuildOptions opts)
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
            string? nextEnvironment = buildRepo.GetNextEnvironment(opts.Environment);
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
            List(new ListBuildsOptions { ProjectName = opts.ProjectName });
        }
        return 0;
    }

    public static int List(ListBuildsOptions? opts = null)
    {
        using (var context = new BuildsContext())
        {
            var searchProjectName = opts?.ProjectName?.ToLower();
            List<Build> builds = context.Builds
                .Include(b => b.Project)
                .Include(b => b.Environment)
                .Where(b => searchProjectName == null || b.Project.Name.ToLower() == searchProjectName)
                .ToList();

            var bare = opts?.Bare ?? false;

            var table = new Table();
            table.AddColumns("Project", "Integration", "Testing", "Production");
            foreach (var project in builds.Select(b => b.Project.Name).Distinct().OrderBy(p => p))
            {
                var integrationBuild = builds.FirstOrDefault(b => b.Project.Name == project && b.Environment?.Name == "Integration");
                var testingBuild = builds.FirstOrDefault(b => b.Project.Name == project && b.Environment?.Name == "Testing");
                var productionBuild = builds.FirstOrDefault(b => b.Project.Name == project && b.Environment?.Name == "Production");
                if (bare)
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
                    var intPanel = DisplayPanel(integrationBuild, bare);
                    var testPanel = DisplayPanel(testingBuild, bare);
                    var prodPanel = DisplayPanel(productionBuild, bare);
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