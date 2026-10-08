using AssimilationSoftware.Buildster.Core;
using AssimilationSoftware.Buildster.Core.Interfaces;
using AssimilationSoftware.Buildster.Core.Model;
using Microsoft.EntityFrameworkCore;
using Spectre.Console;

namespace AssimilationSoftware.Buildster.CLI.Controllers;

public class MachinesController
{
    private DbContextOptions<BuildsContext> _contextOptions;
    private readonly IStatusWriter _statusWriter;

    public MachinesController(IStatusWriter statusWriter, DbContextOptions<BuildsContext>? dbContextOptions = null)
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

    public int Add(string name, string? description)
    {
        using (var context = new BuildsContext(_contextOptions))
        {
            var machine = new Machine()
            {
                Name = name,
                Description = description
            };
            context.Machines.Add(machine);
            context.SaveChanges();
            List();
        }
        return 0;
    }

    public int Delete(string name)
    {
        using (var context = new BuildsContext(_contextOptions))
        {
            var machine = context.Machines.FirstOrDefault(m => m.Name.ToLower() == name.ToLower());
            if (machine is null)
            {
                _statusWriter.Write($"Machine {name} not found");
                return 0;
            }
            context.Machines.Remove(machine);
            // Remove related data, too.
            context.EnvironmentPaths.RemoveRange(context.EnvironmentPaths.Where(ep => ep.MachineId == machine.MachineId));
            context.ProjectPaths.RemoveRange(context.ProjectPaths.Where(pp => pp.MachineId == machine.MachineId));
            List();
        }
        return 0;
    }

    public int Update(string originalName, string? updatedName, string? updatedDescription)
    {
        // Find the machine.
        using (var context = new BuildsContext(_contextOptions))
        {
            var machine = context.FindMachine(originalName);
            if (machine is null)
            {
                _statusWriter.Write($"Machine not found: {originalName}");
                return 0;
            }
            if (!string.IsNullOrWhiteSpace(updatedName))
            {
                machine.Name = updatedName;
            }
            if (!string.IsNullOrWhiteSpace(updatedDescription))
            {
                machine.Description = updatedDescription;
            }
            context.SaveChanges();
            List();
        }
        return 0;
    }

    public int List()
    {
        using (var context = new BuildsContext(_contextOptions))
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
}