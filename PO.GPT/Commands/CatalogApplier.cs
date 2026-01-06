using Karambolo.PO;

namespace PO.GPT.Commands;

public class CatalogApplier : ICatalogApplier
{
    public POCatalog Apply(
        POCatalog catalog,
        IReadOnlyList<TranslationResult> results,
        string language
    )
    {
        if (results.Count == 0) return catalog;

        var unitDictionary = catalog.Values.ToDictionary(
            u => u.Key.Id,
            u => u
        );

        foreach (var result in results)
            unitDictionary[result.OriginalUnit.MsgId] = new POPluralEntry(new POKey(
                result.TranslatedUnit.MsgId,
                result.TranslatedUnit.PluralId,
                result.TranslatedUnit.Context
            ));

        var final = new POCatalog
        {
            HeaderComments = catalog.HeaderComments,
            Encoding = "UTF-8",
            Language = language,
            Headers = new Dictionary<string, string>
            {
                { "CREATION", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
                { "PO.GPT", "1.0.0" }
            }
        };

        foreach (var unit in unitDictionary) final.Add(unit.Value);

        return final;
    }
}