using AssimilationSoftware.Buildster.CLI.Options;
using AssimilationSoftware.Buildster.Core;
using AssimilationSoftware.Buildster.Core.Interfaces;
using AssimilationSoftware.Buildster.Core.Model;
using AssimilationSoftware.Buildster.Core.Utils;
using Microsoft.EntityFrameworkCore;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace AssimilationSoftware.Buildster.CLI.Controllers;

public class BuildsController
{
    private DbContextOptions<BuildsContext> _contextOptions;
    private readonly IStatusWriter _statusWriter;

    public BuildsController(IStatusWriter statusWriter, DbContextOptions<BuildsContext>? dbContextOptions = null)
    {
        _statusWriter = statusWriter;
        if (dbContextOptions == null)
        {
            _contextOptions = new DbContextOptionsBuilder<BuildsContext>()
            .UseSqlite("Data Source=Buildster.sqlite")
            .Options;
        }
        else
        {
            _contextOptions = dbContextOptions;
        }
    }

    public int Add(AddBuildOptions opts)
    {
        using (var context = new BuildsContext(_contextOptions))
        {
            // Get the project by name.
            var project = context.FindProject(opts.ProjectName);
            if (project is null)
            {
                _statusWriter.Write($"Cannot find a project with the name {opts.ProjectName}");
                return 0;
            }
            var integration = context.FindEnvironment("Integration")!;
            var build = new Build()
            {
                Timestamp = opts.BuildDate ?? DateTime.Now,
                Version = opts.Version,
                Environment = integration,
                Notes = opts.Description,
                Project = project
            };
            var path = context.FindProjectPath(project, System.Environment.MachineName);
            if (path is null)
            {
                _statusWriter.Write($"Cannot find a path for project {project.Name} on this machine.");
                return 0;
            }
            var version = new VersionNumber(opts.Version);
            // Reject any build currently in the environment.
            context.Builds.RemoveRange(context.Builds.Where(b => b.ProjectId == project.ProjectId && b.EnvironmentId == integration.EnvironmentId));

            if (!opts.DataOnly)
            {
                VersionInfo.Update(path.Path, version, _statusWriter);
                // Add tag to source control, push tag to origin if present
                GitUtils.Tag(path.Path, version, _statusWriter);
                // Update copyright if needed
                var company = VersionInfo.GetCompany(path.Path, _statusWriter).FirstOrDefault() ?? string.Empty;
                VersionInfo.UpdateCopyright(path.Path, company, DateTime.Now.Year, _statusWriter);
                // Add release notes
                ReleaseNotes.AppendNotes(path.Path, DateTime.Now, version, opts.Description?.Split(['.'], StringSplitOptions.RemoveEmptyEntries) ?? []);
                // TODO: build the actual packages
                // (first need to store package info in the database)
            }

            context.Builds.Add(build);
            context.SaveChanges();
            List();
        }
        return 0;
    }

    public int Delete(DeleteBuildOptions opts)
    {
        using (var context = new BuildsContext(_contextOptions))
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
                _statusWriter.Write($"Build not found in {opts.EnvironmentName} for {opts.ProjectName}");
                return 0;
            }
            // TODO: Perhaps mark a build as rejected once we have build history in place. Will require a new property.
            context.Builds.Remove(build);
            context.SaveChanges();
            if (!opts.DataOnly)
            {
                // TODO: Attempt to delete the build files from disk.
            }
            if (build.Environment is null)
            {
                _statusWriter.Write($"Build {build.Version} removed from {build.Project.Name}");
            }
            else
            {
                _statusWriter.Write($"Build {build.Version} removed from {build.Environment.Name} for {build.Project.Name}");
            }
        }
        return 0;
    }

    public int Update(UpdateBuildOptions opts)
    {
        using (var buildRepo = new BuildsContext(_contextOptions))
        {
            // Check that the build exists.
            var build = buildRepo.FindDeployedBuild(opts.ProjectName, opts.Environment);
            if (build == null)
            {
                _statusWriter.Write($"No build found in '{opts.Environment}' for project '{opts.ProjectName}'.");
                return 1;
            }
            // 1. Get the next environment.
            string? nextEnvironment = buildRepo.GetNextEnvironment(opts.Environment);
            if (string.IsNullOrEmpty(nextEnvironment))
            {
                _statusWriter.Write($"No next environment found after '{opts.Environment}'.");
                return 1;
            }
            // 2. Reject any existing build in the next environment.
            var existingBuild = buildRepo.FindDeployedBuild(opts.ProjectName, nextEnvironment);
            if (existingBuild != null)
            {
                buildRepo.Builds.Remove(existingBuild);
                if (!opts.DataOnly)
                {
                    // TODO: Delete existing files.
                }
                _statusWriter.Write($"Existing build '{existingBuild.Version}' for project '{opts.ProjectName}' in environment '{nextEnvironment}' rejected.");
            }
            // 3. Promote the build to the next environment.
            var environment = buildRepo.FindEnvironment(nextEnvironment);
            if (environment == null)
            {
                _statusWriter.Write($"Could not find environment '{nextEnvironment}'.");
                return 1;
            }
            build.EnvironmentId = environment?.EnvironmentId;
            buildRepo.Update(build);
            buildRepo.SaveChanges();
            if (!opts.DataOnly)
            {
                // TODO: Move build files to next environment.
            }
            _statusWriter.Write($"Build '{build.Version}' for project '{opts.ProjectName}' promoted to '{nextEnvironment}'.");
            List(new ListBuildsOptions { ProjectName = opts.ProjectName });
        }
        return 0;
    }

    public int List(ListBuildsOptions? opts = null)
    {
        using (var context = new BuildsContext(_contextOptions))
        {
            var searchProjectName = opts?.ProjectName?.ToLower();
            List<Build> builds = context.Builds
                .Include(b => b.Project)
                .Include(b => b.Environment)
                .Where(b => searchProjectName == null || b.Project.Name.ToLower() == searchProjectName)
                .ToList();

            var bare = opts?.Bare ?? false;
            var pending = opts?.Pending ?? false;

            var table = new Table();
            if (pending)
            {
                table.AddColumns("Project", "Pending", "Integration", "Testing", "Production");
            }
            else
            {
                table.AddColumns("Project", "Integration", "Testing", "Production");
            }
            var firstRow = true;
            foreach (var project in context.Projects.Where(b => searchProjectName == null || b.Name.ToLower() == searchProjectName).Select(p => p.Name).Distinct().OrderBy(p => p))
            {
                IEnumerable<string> gitMessages = Enumerable.Empty<string>();
                if (pending)
                {
                    var path = context.FindProjectPath(context.FindProject(project)!, System.Environment.MachineName);
                    // Get recent Git history for the main branch and show a list of commit messages
                    if (Directory.Exists(path.Path))
                    {
                        gitMessages = GitUtils.GetRecentGitHistory(path.Path);
                    }
                }
                var integrationBuild = builds.FirstOrDefault(b => b.Project.Name == project && b.Environment?.Name == "Integration");
                var testingBuild = builds.FirstOrDefault(b => b.Project.Name == project && b.Environment?.Name == "Testing");
                var productionBuild = builds.FirstOrDefault(b => b.Project.Name == project && b.Environment?.Name == "Production");
                if (bare)
                {
                    var row = new List<string> { project };
                    if (pending)
                    {
                        row.Add(Markup.Escape(string.Join(System.Environment.NewLine, gitMessages)));
                    }
                    row.Add(integrationBuild?.Version ?? string.Empty);
                    row.Add(testingBuild?.Version ?? string.Empty);
                    row.Add(productionBuild?.Version ?? string.Empty);
                    if (!firstRow)
                    {
                        table.AddEmptyRow();
                    }
                    table.AddRow(row.ToArray());
                }
                else
                {
                    var row = new List<IRenderable> { new Markup(project) };
                    if (pending)
                    {
                        if (!gitMessages.Any())
                        {
                            row.Add(new Markup("-"));
                        }
                        else
                        {
                            var pendingPanel = new Panel(Markup.Escape(string.Join(System.Environment.NewLine, gitMessages)));
                            row.Add(pendingPanel);
                        }
                    }
                    row.Add(DisplayPanel(integrationBuild, bare));
                    row.Add(DisplayPanel(testingBuild, bare));
                    row.Add(DisplayPanel(productionBuild, bare));
                    table.AddRow(row.ToArray());
                }
                firstRow = false;
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