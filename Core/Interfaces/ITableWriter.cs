namespace AssimilationSoftware.Buildster.Core.Interfaces;

public interface ITableWriter
{
    void Write(TableDescription table);
}

public sealed record TableDescription(
    IReadOnlyList<string> Columns,
    IReadOnlyList<TableRow> Rows);

public sealed record TableRow(
    IReadOnlyList<TableCell> Cells,
    bool IsSeparator = false)
{
    public static TableRow Separator { get; } = new(Array.Empty<TableCell>(), true);
}

public sealed record TableCell(string Text, bool IsFramed = false);