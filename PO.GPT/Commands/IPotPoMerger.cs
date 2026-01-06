using Karambolo.PO;

namespace PO.GPT.Commands;

public interface IPotPoMerger
{
    MergeResult Merge(POCatalog pot, POCatalog existingPo);
}

public record MergeResult(
    POCatalog BaseCatalog,
    IReadOnlyList<TranslationUnit> Missing
);