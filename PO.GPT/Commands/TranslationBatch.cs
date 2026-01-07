namespace PO.GPT.Commands;

public record TranslationBatch(
    IReadOnlyList<TranslationUnit> Units
);