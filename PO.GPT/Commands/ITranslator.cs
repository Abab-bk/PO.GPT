namespace PO.GPT.Commands;

public interface ITranslator
{
    Task<IReadOnlyList<TranslationUnit>> TranslateAsync(
        IReadOnlyList<TranslationUnit> batch,
        string targetLanguage,
        string userPrompt,
        CancellationToken ct
    );
}