namespace PO.GPT.Commands;

public interface ITranslationPlanner
{
    IReadOnlyList<TranslationBatch> Plan(
        IReadOnlyList<TranslationUnit> units,
        int batchSize
    );
}

public record TranslationBatch(
    IReadOnlyList<TranslationUnit> Units
);