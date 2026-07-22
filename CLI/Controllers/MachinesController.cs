using AssimilationSoftware.Buildster.CLI.Options;
using AssimilationSoftware.Buildster.Core;
using AssimilationSoftware.Buildster.Core.Model;
using Spectre.Console;

namespace AssimilationSoftware.Buildster.CLI.Controllers;

public class MachinesController
{

    public static int Add(AddMachineOptions opts)
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
            List();
        }
        return 0;
    }

    public static int Delete(DeleteMachineOptions opts)
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
            List();
        }
        return 0;
    }

    public static int Update(UpdateMachineOptions opts)
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
            List();
        }
        return 0;
    }

    public static int List(ListMachinesOptions? opts = null)
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
}