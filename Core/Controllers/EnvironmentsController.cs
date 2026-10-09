using AssimilationSoftware.Buildster.Core;
using AssimilationSoftware.Buildster.Core.Interfaces;
using AssimilationSoftware.Buildster.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace AssimilationSoftware.Buildster.Core.Controllers;

public class EnvironmentsController
{
    private DbContextOptions<BuildsContext> _contextOptions;
    private readonly IStatusWriter _statusWriter;
    private readonly ITableWriter _tableWriter;

    public EnvironmentsController(IStatusWriter statusWriter, ITableWriter tableWriter, DbContextOptions<BuildsContext>? dbContextOptions = null)
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

    public int SetPath(string projectName, string? machineName, string environmentName, string folder)
    {
        using (var context = new BuildsContext(_contextOptions))
        {
            var project = context.FindProject(projectName);
            var machine = string.IsNullOrEmpty(machineName) ? context.FindMachine(System.Environment.MachineName) : context.FindMachine(machineName);
            var env = context.FindEnvironment(environmentName);
            if (project is null)
            {
                _statusWriter.Write($"Could not find project {projectName}");
                return 0;
            }
            if (machine is null)
            {
                _statusWriter.Write($"Could not find machine {machineName}");
                return 0;
            }
            if (env is null)
            {
                _statusWriter.Write($"Could not find environment {environmentName}");
                return 0;
            }
            var envPath = context.FindEnvironmentPath(project.Name, machine.Name, env.Name);
            if (envPath is null)
            {
                envPath = new EnvironmentPath()
                {
                    Path = folder,
                    EnvironmentId = env.EnvironmentId,
                    MachineId = machine.MachineId,
                    ProjectId = project.ProjectId
                };
                context.Add(envPath);
            }
            else
            {
                envPath.Path = folder;
                context.Update(envPath);
            }
            context.SaveChanges();
            List(projectName, machineName);
            return 0;
        }
    }

    public int List(string? projectName = null, string? machineName = null)
    {
        using (var context = new BuildsContext(_contextOptions))
        {
            var envPaths = context.EnvironmentPaths.Include(ep => ep.Project).Include(ep => ep.Machine).Include(ep => ep.Environment)
                .Where(ep => (string.IsNullOrWhiteSpace(projectName) || ep.Project.Name.ToLower() == projectName.ToLower())
                && (string.IsNullOrWhiteSpace(machineName) || ep.Machine.Name.ToLower() == machineName.ToLower()));

            // For each project and machine, add a row to the table showing the given environment path.
            // If there is no path for the given environment, project, and machine, show an empty placeholder like "-".
            Dictionary<(string Project, string Machine), (string Integration, string Testing, string Production)> rowData = new();
            foreach (var ep in envPaths)
            {
                var key = (ep.Project.Name, ep.Machine.Name);
                // Add a table row for the project and machine, but what are the paths?
                if (!rowData.TryGetValue(key, out var paths))
                {
                    // Add entry.
                    paths = ("-", "-", "-");
                }
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

            List<TableRow> rows = [];
            foreach (var row in rowData)
            {
                rows.Add(new TableRow(
                [
                    new TableCell(row.Key.Project),
                    new TableCell(row.Key.Machine),
                    new TableCell(row.Value.Integration),
                    new TableCell(row.Value.Testing),
                    new TableCell(row.Value.Production)
                ]));
            }
            if (rowData.Count > 0)
            {
                _tableWriter.Write(new TableDescription(
                    ["Project", "Machine", "Integration", "Testing", "Production"],
                    rows));
            }
            else
            {
                _statusWriter.Write("No data to display");
            }
            return 0;
        }
    }
}