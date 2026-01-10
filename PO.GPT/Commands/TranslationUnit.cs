namespace PO.GPT.Commands;

public record TranslationUnit(
    string MsgId,
    string? Context = null,
    string? PluralId = null,
    string ExistingTranslation = ""
)
{
    public bool IsTranslated => !string.IsNullOrEmpty(ExistingTranslation);

    public TranslationUnit WithTranslation(string translation)
    {
        return this with { ExistingTranslation = translation };
    }
}