using Spectre.Console;

namespace PO.GPT.Commands;

public class DryRunTranslator(TokenCounter tokenCounter, IAnsiConsole console)
    : ITranslator
{
    public Task<IReadOnlyList<TranslationUnit>> TranslateAsync(
        IReadOnlyList<TranslationUnit> batch,
        string targetLanguage,
        string userPrompt,
        CancellationToken ct)
    {
        var promptTokens = TokenEstimator.EstimateBatchTokens(batch, targetLanguage);
        var completionTokens = batch.Sum(u => TokenEstimator.EstimateTokens(u.MsgId));

        if (!string.IsNullOrEmpty(userPrompt))
            console.MarkupLine($"Your prompt: {userPrompt}");

        tokenCounter.AddUsage(promptTokens, completionTokens);

        console.MarkupLine($"[grey]✓ Simulated ({promptTokens + completionTokens} tokens)[/]");

        var results = batch
            .Select(u => u.WithTranslation($"[{targetLanguage}] {u.MsgId}"))
            .ToList();

        return Task.FromResult<IReadOnlyList<TranslationUnit>>(results);
    }
}