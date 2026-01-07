namespace PO.GPT.Commands;

public class DryRunTranslator : ITranslator
{
    public Task<IReadOnlyList<TranslationResult>> TranslateAsync(
        IReadOnlyList<TranslationUnit> batch,
        string targetLanguage,
        CancellationToken ct)
    {
        var results = batch
            .Select(u => new TranslationResult(
                u,
                $"[{targetLanguage}] {u.MsgId}"))
            .ToList();

        return Task.FromResult<IReadOnlyList<TranslationResult>>(results);
    }
}