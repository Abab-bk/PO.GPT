namespace PO.GPT.Commands;

public class TranslationPlanner
{
    public IReadOnlyList<TranslationBatch> Plan(
        IReadOnlyList<TranslationUnit> units,
        int batchSize
    )
    {
        if (batchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than zero.");

        if (units.Count == 0) return [];

        return units
            .Chunk(batchSize)
            .Select(chunk => new TranslationBatch(chunk.ToList()))
            .ToList();
    }
}