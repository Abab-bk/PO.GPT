namespace PO.GPT.Commands;

public class TranslationPlanner
{
    public IReadOnlyList<IReadOnlyList<TranslationUnit>> Plan(
        IReadOnlyList<TranslationUnit> units,
        int batchSize)
    {
        if (batchSize <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(batchSize),
                "Batch size must be greater than zero.");

        if (units.Count == 0) return [];

        return units
            .Chunk(batchSize)
            .Select(chunk => (IReadOnlyList<TranslationUnit>)chunk.ToList())
            .ToList();
    }
}