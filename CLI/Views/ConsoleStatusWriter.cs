using AssimilationSoftware.Buildster.Core.Interfaces;

namespace AssimilationSoftware.Buildster.CLI.Views;

public class ConsoleStatusWriter : IStatusWriter
{
    public void Write(string message)
    {
        Console.WriteLine(message);
    }
}