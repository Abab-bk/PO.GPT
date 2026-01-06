namespace PO.GPT.Commands;

public interface ITranslator
{
    Task<IReadOnlyList<TranslationResult>> TranslateAsync(
        IReadOnlyList<TranslationUnit> batch,
        string targetLanguage,
        CancellationToken ct
    );
}