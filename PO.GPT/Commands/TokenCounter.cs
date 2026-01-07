using Spectre.Console;

namespace PO.GPT.Commands;

public class TokenCounter
{
    private readonly object _lock = new();
    private int _totalCompletionTokens;
    private int _totalPromptTokens;

    public void AddUsage(int promptTokens, int completionTokens)
    {
        lock (_lock)
        {
            _totalPromptTokens += promptTokens;
            _totalCompletionTokens += completionTokens;
        }
    }

    public TokenUsage GetUsage()
    {
        return new TokenUsage(
            _totalPromptTokens,
            _totalCompletionTokens,
            _totalPromptTokens + _totalCompletionTokens
        );
    }

    public void RenderSummary(IAnsiConsole console, string modelName)
    {
        var usage = GetUsage();
        var cost = CalculateCost(usage, modelName);

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Blue)
            .AddColumn("[bold]Token Usage[/]")
            .AddColumn("[bold]Amount[/]", c => c.RightAligned());

        table.AddRow("Prompt tokens", usage.PromptTokens.ToString("N0"));
        table.AddRow("Completion tokens", usage.CompletionTokens.ToString("N0"));
        table.AddRow("[bold]Total tokens[/]", usage.TotalTokens.ToString("N0"));
        table.AddEmptyRow();
        table.AddRow("[bold green]Estimated cost[/]", $"${cost:F4}");

        console.WriteLine();
        console.Write(
            new Panel(table)
                .Header("[bold blue]💰 Token Statistics[/]")
                .BorderColor(Color.Blue)
        );
    }

    private static decimal CalculateCost(TokenUsage usage, string model)
    {
        var (inputRate, outputRate) = model.ToLower() switch
        {
            var m when m.Contains("gpt-4") => (0.03m, 0.06m),
            var m when m.Contains("gpt-3.5") => (0.0015m, 0.002m),
            var m when m.Contains("claude-3-5-sonnet") => (0.003m, 0.015m),
            var m when m.Contains("claude-3-opus") => (0.015m, 0.075m),
            var m when m.Contains("claude-3-sonnet") => (0.003m, 0.015m),
            var m when m.Contains("claude-3-haiku") => (0.00025m, 0.00125m),
            _ => (0.002m, 0.002m)
        };

        return usage.PromptTokens / 1000m * inputRate +
               usage.CompletionTokens / 1000m * outputRate;
    }
}

public record TokenUsage(
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens
);