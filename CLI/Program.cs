using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using AssimilationSoftware.Buildster.CLI.Options;
using AssimilationSoftware.Buildster.Core;
using AssimilationSoftware.Buildster.Core.Model;
using CommandLine;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Spectre.Console;
namespace AssimilationSoftware.Buildster.CLI;

public class Program
{
    private const string Integration = "Integration";

    public static int Main(string[] args)
    {
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
            (UpdateMachineOptions opts) => UpdateMachine(opts),
            (UpdateProjectOptions opts) => UpdateProject(opts),
            errs => 1);
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
            var integration = context.FindEnvironment(Integration, createIfNotFound: true);
            var build = new Build()
            {
                Timestamp = DateTime.Now,
                Version = opts.Version,
                Environment = integration,
                Notes = opts.Description,
                Project = project
            };
            // TODO: Add tag to source control, push tag to origin if present, update version numbers, update copyright year if needed, add release notes
            // TODO: Reject any build currently in the environment.
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
                var currentMachine = context.Machines.FirstOrDefault(m => m.Name == System.Environment.MachineName);
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
            foreach (var build in context.Builds.Include(b => b.Project).Include(b => b.Environment))
            {
                Console.WriteLine($"{build.Project.Name}: {build.Version} ({build.Environment?.Name ?? "Rejected"})");
            }
        }
        return 0;
    }

    public static int ListMachines(ListMachinesOptions? opts = null)
    {
        using (var context = new BuildsContext())
        {
            foreach (var machine in context.Machines)
            {
                Console.WriteLine($"{machine.Name} - {machine.Description}");
            }
        }
        return 0;
    }

    public static int ListProjects(ListProjectsOptions? opts = null)
    {
        using (var context = new BuildsContext())
        {
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
                    Console.WriteLine($"{proj.Name} @ {path?.Path}");
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