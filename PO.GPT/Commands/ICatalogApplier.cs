using Karambolo.PO;

namespace PO.GPT.Commands;

public interface ICatalogApplier
{
    POCatalog Apply(
        POCatalog catalog,
        IReadOnlyList<TranslationResult> results,
        string language
    );
}