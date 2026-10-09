using AssimilationSoftware.Buildster.Core;
using AssimilationSoftware.Buildster.Core.Interfaces;
using AssimilationSoftware.Buildster.Core.Model;
using AssimilationSoftware.Buildster.Core.Utils;
using Microsoft.EntityFrameworkCore;

namespace AssimilationSoftware.Buildster.Core.Controllers;

public class BuildsController
{
    private DbContextOptions<BuildsContext> _contextOptions;
    private readonly IStatusWriter _statusWriter;
    private readonly ITableWriter _tableWriter;

    public BuildsController(IStatusWriter statusWriter, ITableWriter tableWriter, DbContextOptions<BuildsContext>? dbContextOptions = null)
    {
        _statusWriter = statusWriter;
        _tableWriter = tableWriter;
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

    public int Add(string projectName, string version, DateTime? buildDate, string? description, bool dataOnly)
    {
        using (var context = new BuildsContext(_contextOptions))
        {
            // Get the project by name.
            var project = context.FindProject(projectName);
            if (project is null)
            {
                _statusWriter.Write($"Cannot find a project with the name {projectName}");
                return 0;
            }
            var integration = context.FindEnvironment("Integration")!;
            var build = new Build()
            {
                Timestamp = buildDate ?? DateTime.Now,
                Version = version,
                Environment = integration,
                Notes = description,
                Project = project
            };
            var path = context.FindProjectPath(project, System.Environment.MachineName);
            if (path is null)
            {
                _statusWriter.Write($"Cannot find a path for project {project.Name} on this machine.");
                return 0;
            }
            var buildVersion = new VersionNumber(version);
            // Reject any build currently in the environment.
            context.Builds.RemoveRange(context.Builds.Where(b => b.ProjectId == project.ProjectId && b.EnvironmentId == integration.EnvironmentId));

            if (!dataOnly)
            {
                VersionInfo.Update(path.Path, buildVersion, _statusWriter);
                // Add tag to source control, push tag to origin if present
                GitUtils.Tag(path.Path, buildVersion, _statusWriter);
                // Update copyright if needed
                var company = VersionInfo.GetCompany(path.Path, _statusWriter).FirstOrDefault() ?? string.Empty;
                VersionInfo.UpdateCopyright(path.Path, company, DateTime.Now.Year, _statusWriter);
                // Add release notes
                ReleaseNotes.AppendNotes(path.Path, DateTime.Now, buildVersion, description?.Split(['.'], StringSplitOptions.RemoveEmptyEntries) ?? []);
                // TODO: build the actual packages
                // (first need to store package info in the database)
            }

            context.Builds.Add(build);
            context.SaveChanges();
            List();
        }
        return 0;
    }

    public int Delete(string projectName, string environmentName, bool dataOnly)
    {
        using (var context = new BuildsContext(_contextOptions))
        {
            var build = context.Builds
                .Include(b => b.Project)
                .Include(b => b.Environment)
                .FirstOrDefault(b =>
                    b.Project.Name.ToLower() == projectName.ToLower() &&
                    b.Environment != null &&
                    b.Environment.Name.ToLower() == environmentName.ToLower()
                );
            if (build is null)
            {
                _statusWriter.Write($"Build not found in {environmentName} for {projectName}");
                return 0;
            }
            // TODO: Perhaps mark a build as rejected once we have build history in place. Will require a new property.
            context.Builds.Remove(build);
            context.SaveChanges();
            if (!dataOnly)
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

    public int Update(string projectName, string sourceEnvironment, bool dataOnly)
    {
        using (var buildRepo = new BuildsContext(_contextOptions))
        {
            // Check that the build exists.
            var build = buildRepo.FindDeployedBuild(projectName, sourceEnvironment);
            if (build == null)
            {
                _statusWriter.Write($"No build found in '{sourceEnvironment}' for project '{projectName}'.");
                return 1;
            }
            // 1. Get the next environment.
            string? nextEnvironment = buildRepo.GetNextEnvironment(sourceEnvironment);
            if (string.IsNullOrEmpty(nextEnvironment))
            {
                _statusWriter.Write($"No next environment found after '{sourceEnvironment}'.");
                return 1;
            }
            // 2. Reject any existing build in the next environment.
            var existingBuild = buildRepo.FindDeployedBuild(projectName, nextEnvironment);
            if (existingBuild != null)
            {
                buildRepo.Builds.Remove(existingBuild);
                if (!dataOnly)
                {
                    // TODO: Delete existing files.
                }
                _statusWriter.Write($"Existing build '{existingBuild.Version}' for project '{projectName}' in environment '{nextEnvironment}' rejected.");
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
            if (!dataOnly)
            {
                // TODO: Move build files to next environment.
            }
            _statusWriter.Write($"Build '{build.Version}' for project '{projectName}' promoted to '{nextEnvironment}'.");
            List(projectName);
        }
        return 0;
    }

    public int List(string? projectName = null, bool bare = false, bool pending = false)
    {
        using (var context = new BuildsContext(_contextOptions))
        {
            var searchProjectName = projectName?.ToLower();
            List<Build> builds = context.Builds
                .Include(b => b.Project)
                .Include(b => b.Environment)
                .Where(b => searchProjectName == null || b.Project.Name.ToLower() == searchProjectName)
                .ToList();

            var columns = pending
                ? new[] { "Project", "Pending", "Integration", "Testing", "Production" }
                : new[] { "Project", "Integration", "Testing", "Production" };
            List<TableRow> rows = [];
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
                    var row = new List<TableCell> { new(project) };
                    if (pending)
                    {
                        row.Add(new TableCell(string.Join(System.Environment.NewLine, gitMessages)));
                    }
                    row.Add(new TableCell(integrationBuild?.Version ?? string.Empty));
                    row.Add(new TableCell(testingBuild?.Version ?? string.Empty));
                    row.Add(new TableCell(productionBuild?.Version ?? string.Empty));
                    if (!firstRow)
                    {
                        rows.Add(TableRow.Separator);
                    }
                    rows.Add(new TableRow(row));
                }
                else
                {
                    var row = new List<TableCell> { new(project) };
                    if (pending)
                    {
                        if (!gitMessages.Any())
                        {
                            row.Add(new TableCell("-"));
                        }
                        else
                        {
                            row.Add(new TableCell(string.Join(System.Environment.NewLine, gitMessages), IsFramed: true));
                        }
                    }
                    row.Add(DisplayCell(integrationBuild));
                    row.Add(DisplayCell(testingBuild));
                    row.Add(DisplayCell(productionBuild));
                    rows.Add(new TableRow(row));
                }
                firstRow = false;
            }
            _tableWriter.Write(new TableDescription(columns, rows));
        }
        return 0;
    }

    private static TableCell DisplayCell(Build? build)
    {
        if (build == null)
        {
            return new TableCell("-");
        }
        return new TableCell($"Version: {build.Version}\nDate: {build.Timestamp:yyyy-MM-dd}\nNotes: {build.Notes}", IsFramed: true);
    }

}