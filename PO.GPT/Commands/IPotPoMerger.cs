using Karambolo.PO;

namespace PO.GPT.Commands;

public interface IPotPoMerger
{
    MergeResult Merge(POCatalog pot, POCatalog existingPo, bool skipTranslated);
}

public record MergeResult(
    POCatalog BaseCatalog,
    IReadOnlyList<TranslationUnit> Missing
);