using Spectre.Console.Cli;

namespace PO.GPT.Commands;

public class VersionCommand : Command
{
    protected override int Execute(CommandContext context, CancellationToken cancellationToken)
    {
        Console.WriteLine("1.1.0");
        return 0;
    }
}