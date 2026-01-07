namespace PO.GPT.Commands;

public record TranslationResult(
    TranslationUnit OriginalUnit,
    string Translated
);