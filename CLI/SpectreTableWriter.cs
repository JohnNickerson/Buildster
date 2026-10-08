using AssimilationSoftware.Buildster.Core.Interfaces;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace AssimilationSoftware.Buildster.CLI;

public sealed class SpectreTableWriter : ITableWriter
{
    public void Write(TableDescription description)
    {
        var table = new Table();
        table.AddColumns(description.Columns.ToArray());

        foreach (var row in description.Rows)
        {
            if (row.IsSeparator)
            {
                table.AddEmptyRow();
                continue;
            }

            IRenderable[] cells = row.Cells
                .Select(cell => cell.IsFramed
                    ? (IRenderable)new Panel(Markup.Escape(cell.Text))
                    : new Markup(Markup.Escape(cell.Text)))
                .ToArray();
            table.AddRow(cells);
        }

        AnsiConsole.Write(table);
    }
}