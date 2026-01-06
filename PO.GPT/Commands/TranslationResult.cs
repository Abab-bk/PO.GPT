namespace PO.GPT.Commands;

public record TranslationResult(
    TranslationUnit OriginalUnit,
    TranslationUnit TranslatedUnit
);