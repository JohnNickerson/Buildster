using AssimilationSoftware.Buildster.CLI.Options;
using AssimilationSoftware.Buildster.Core;
using AssimilationSoftware.Buildster.Core.Model;
using Microsoft.EntityFrameworkCore;
using Spectre.Console;

namespace AssimilationSoftware.Buildster.CLI.Controllers;

public class EnvironmentsController
{
    public static int SetPath(SetEnvironmentPathOptions opts)
    {
        using (var context = new BuildsContext())
        {
            var project = context.FindProject(opts.ProjectName);
            var machine = string.IsNullOrEmpty(opts.MachineName) ? context.FindMachine(System.Environment.MachineName) : context.FindMachine(opts.MachineName);
            var env = context.FindEnvironment(opts.EnvironmentName);
            if (project is null)
            {
                Console.WriteLine($"Could not find project {opts.ProjectName}");
                return 0;
            }
            if (machine is null)
            {
                Console.WriteLine($"Could not find machine {opts.MachineName}");
                return 0;
            }
            if (env is null)
            {
                Console.WriteLine($"Could not find environment {opts.EnvironmentName}");
                return 0;
            }
            var envPath = context.FindEnvironmentPath(project.Name, machine.Name, env.Name);
            if (envPath is null)
            {
                envPath = new EnvironmentPath()
                {
                    Path = opts.Folder,
                    EnvironmentId = env.EnvironmentId,
                    MachineId = machine.MachineId,
                    ProjectId = project.ProjectId
                };
                context.Add(envPath);
            }
            else
            {
                envPath.Path = opts.Folder;
                context.Update(envPath);
            }
            context.SaveChanges();
            List(new() { MachineName = opts.MachineName, ProjectName = opts.ProjectName });
            return 0;
        }
    }

    internal static int List(ListEnvironmentPathsOptions opts)
    {
        using (var context = new BuildsContext())
        {
            var envPaths = context.EnvironmentPaths.Include(ep => ep.Project).Include(ep => ep.Machine).Include(ep => ep.Environment)
                .Where(ep => (string.IsNullOrWhiteSpace(opts.ProjectName) || ep.Project.Name.ToLower() == opts.ProjectName.ToLower())
                && (string.IsNullOrWhiteSpace(opts.MachineName) || ep.Machine.Name.ToLower() == opts.MachineName.ToLower()));

            // For each project and machine, add a row to the table showing the given environment path.
            // If there is no path for the given environment, project, and machine, show an empty placeholder like "-".
            Dictionary<(string Project, string Machine), (string Integration, string Testing, string Production)> rowData = new();
            foreach (var ep in envPaths)
            {
                var key = (ep.Project.Name, ep.Machine.Name);
                // Add a table row for the project and machine, but what are the paths?
                if (!rowData.ContainsKey(key))
                {
                    // Add entry.
                    rowData[key] = new() { Integration = "-", Testing = "-", Production = "-" };
                }
                var paths = rowData[key];
                switch (ep.Environment.Name)
                {
                    case "Integration":
                        paths.Integration = ep.Path;
                        break;
                    case "Testing":
                        paths.Testing = ep.Path;
                        break;
                    case "Production":
                        paths.Production = ep.Path;
                        break;
                }
                rowData[key] = paths;
            }

            var table = new Spectre.Console.Table();
            table.AddColumns("Project", "Machine", "Integration", "Testing", "Production");
            foreach (var row in rowData)
            {
                table.AddRow(row.Key.Project, row.Key.Machine, row.Value.Integration, row.Value.Testing, row.Value.Production);
            }
            if (rowData.Count > 0)
            {
                AnsiConsole.Write(table);
            }
            else
            {
                Console.WriteLine("No data to display");
            }
            return 0;
        }
    }
}