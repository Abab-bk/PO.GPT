using Spectre.Console;

namespace PO.GPT.Commands;

public class DryRunTranslator(TokenCounter tokenCounter, IAnsiConsole console, string model)
    : ITranslator
{
    public async Task<IReadOnlyList<TranslationResult>> TranslateAsync(
        IReadOnlyList<TranslationUnit> batch,
        string targetLanguage,
        CancellationToken ct)
    {
        await console.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("yellow"))
            .StartAsync("[yellow]🤔 Simulating AI thinking...[/]", async ctx => { await Task.Delay(500, ct); });

        var promptTokens = TokenEstimator.EstimateBatchTokens(batch, targetLanguage);
        var completionTokens = batch.Sum(u => TokenEstimator.EstimateTokens(u.MsgId));

        tokenCounter.AddUsage(promptTokens, completionTokens);

        console.MarkupLine($"[grey]✓ Simulated ({promptTokens + completionTokens} tokens)[/]");

        var results = batch
            .Select(u => new TranslationResult(
                u,
                $"[{targetLanguage}] {u.MsgId}"))
            .ToList();

        return results;
    }
}