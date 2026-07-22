using AssimilationSoftware.Buildster.CLI.Options;
using AssimilationSoftware.Buildster.Core;
using AssimilationSoftware.Buildster.Core.Model;
using Microsoft.EntityFrameworkCore;
using Spectre.Console;

namespace AssimilationSoftware.Buildster.CLI.Controllers;

public class ProjectsController
{
    
    public static int Add(AddProjectOptions opts)
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
            List();
        }
        return 0;
    }

    public static int Delete(DeleteProjectOptions opts)
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
            List();
        }
        return 0;
    }

    public static int Update(UpdateProjectOptions opts)
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
            List();
        }
        return 0;
    }

    public static int List(ListProjectsOptions? opts = null)
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

}