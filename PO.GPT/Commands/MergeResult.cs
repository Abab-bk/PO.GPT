using Karambolo.PO;

namespace PO.GPT.Commands;

public record MergeResult(
    POCatalog BaseCatalog,
    IReadOnlyList<TranslationUnit> Missing
);