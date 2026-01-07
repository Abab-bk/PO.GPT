using Spectre.Console;

namespace PO.GPT.Commands;

public class DryRunTranslator(TokenCounter tokenCounter, IAnsiConsole console)
    : ITranslator
{
    public Task<IReadOnlyList<TranslationResult>> TranslateAsync(
        IReadOnlyList<TranslationUnit> batch,
        string targetLanguage,
        string userPrompt,
        CancellationToken ct)
    {
        var promptTokens = TokenEstimator.EstimateBatchTokens(batch, targetLanguage);
        var completionTokens = batch.Sum(u => TokenEstimator.EstimateTokens(u.MsgId));

        console.MarkupLine($"Your prompt: {userPrompt}");

        tokenCounter.AddUsage(promptTokens, completionTokens);

        console.MarkupLine($"[grey]✓ Simulated ({promptTokens + completionTokens} tokens)[/]");

        var results = batch
            .Select(u => new TranslationResult(
                u,
                $"[{targetLanguage}] {u.MsgId}"))
            .ToList();

        return Task.FromResult<IReadOnlyList<TranslationResult>>(results);
    }
}