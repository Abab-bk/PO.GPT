namespace PO.GPT.Commands;

public class DryRunTranslator : ITranslator
{
    public Task<IReadOnlyList<TranslationResult>> TranslateAsync(
        IReadOnlyList<TranslationUnit> batch,
        string targetLanguage,
        CancellationToken ct
    )
    {
        return Task.FromResult<IReadOnlyList<TranslationResult>>(
            batch.Select(u => new TranslationResult(
                    u,
                    new TranslationUnit(
                        u.MsgId,
                        u.PluralId,
                        u.Context
                    )
                )
            ).ToList()
        );
    }
}