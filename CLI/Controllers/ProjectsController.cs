using AssimilationSoftware.Buildster.Core;
using AssimilationSoftware.Buildster.Core.Interfaces;
using AssimilationSoftware.Buildster.Core.Model;
using Microsoft.EntityFrameworkCore;
using Spectre.Console;

namespace AssimilationSoftware.Buildster.CLI.Controllers;

public class ProjectsController
{
    private DbContextOptions<BuildsContext> _contextOptions;
    private readonly IStatusWriter _statusWriter;

    public ProjectsController(IStatusWriter statusWriter, DbContextOptions<BuildsContext>? dbContextOptions = null)
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

    public int Add(string name, string? description, string? sourceFolder)
    {
        using (var context = new BuildsContext(_contextOptions))
        {
            var project = new Project()
            {
                Name = name,
                Description = description
            };
            context.Projects.Add(project);
            if (sourceFolder is not null)
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
                    Path = sourceFolder,
                    Machine = currentMachine,
                    Project = project
                };
                context.ProjectPaths.Add(projectPath);
            }
            context.SaveChanges();
            List();
        }
        return 0;
    }

    public int Delete(string name)
    {
        using (var context = new BuildsContext(_contextOptions))
        {
            var project = context.FindProject(name);
            if (project is null)
            {
                _statusWriter.Write($"Cannot find project {name}");
                return 0;
            }
            context.Projects.Remove(project);
            context.Builds.RemoveRange(context.Builds.Where(b => b.ProjectId == project.ProjectId));
            context.ProjectPaths.RemoveRange(context.ProjectPaths.Where(pp => pp.ProjectId == project.ProjectId));
            context.Packages.RemoveRange(context.Packages.Where(p => p.ProjectId == project.ProjectId));
            context.EnvironmentPaths.RemoveRange(context.EnvironmentPaths.Where(ep => ep.ProjectId == project.ProjectId));
            context.SaveChanges();
            List();
        }
        return 0;
    }

    public int Update(string searchName, string? updatedName, string? updatedDescription, string? updatedSourcePath, string sourcePathMachine)
    {
        using (var context = new BuildsContext(_contextOptions))
        {
            var project = context.FindProject(searchName);
            if (project is null)
            {
                _statusWriter.Write($"Project not found: {searchName}");
                return 0;
            }
            if (!string.IsNullOrEmpty(updatedName))
            {
                project.Name = updatedName;
            }
            if (!string.IsNullOrEmpty(updatedDescription))
            {
                project.Description = updatedDescription;
            }
            if (!string.IsNullOrEmpty(updatedSourcePath))
            {
                var machine = context.FindMachine(sourcePathMachine);
                if (machine is null)
                {
                    _statusWriter.Write($"Could not find target machine: {sourcePathMachine}");
                    return 0;
                }
                // Update or add source path.
                context.UpdateProjectPath(project, machine, updatedSourcePath);
            }
            context.SaveChanges();
            List();
        }
        return 0;
    }

    public int List(bool verbose = false)
    {
        using (var context = new BuildsContext(_contextOptions))
        {
            Table table = new Table();
            if (verbose)
            {
                table.AddColumns("Project", "Description", "Machine", "Path");
            }
            else
            {
                table.AddColumns("Project", "Path");
            }
            bool firstRow = true;
            foreach (var proj in context.Projects.OrderBy(p => p.Name))
            {
                if (firstRow)
                {
                    firstRow = false;
                }
                else
                {
                    table.AddEmptyRow();
                }
                if (verbose)
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
                    table.AddRow(proj.Name, path?.Path ?? "(no path found)");
                }
            }
            AnsiConsole.Write(table);
        }
        return 0;
    }

    internal int SetCopyright(string projectName, string companyName, string yearString)
    {
        using (var context = new BuildsContext(_contextOptions))
        {
            var project = context.FindProject(projectName);
            if (project is null)
            {
                _statusWriter.Write($"Project not found: {projectName}");
                return 0;
            }
            var path = context.FindProjectPath(project, System.Environment.MachineName);
            if (path is null)
            {
                _statusWriter.Write($"No source path found for project {projectName} on this machine.");
                return 0;
            }
            Core.Utils.VersionInfo.UpdateCopyright(path.Path, companyName, DateTime.Now.Year, _statusWriter);
            context.SaveChanges();
            List();
        }
        return 0;
    }
}