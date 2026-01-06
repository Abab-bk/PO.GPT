using OpenAI.Chat;

namespace PO.GPT.Commands;

public class AiTranslator(ChatClient client) : ITranslator
{
    public Task<IReadOnlyList<TranslationResult>> TranslateAsync(
        IReadOnlyList<TranslationUnit> batch,
        string targetLanguage,
        CancellationToken ct
    )
    {
        throw new NotImplementedException();
    }
}