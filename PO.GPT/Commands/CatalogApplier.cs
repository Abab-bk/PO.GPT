using Karambolo.PO;
using Spectre.Console;

namespace PO.GPT.Commands;

public class CatalogApplier(IAnsiConsole console) : ICatalogApplier
{
    public POCatalog Apply(
        POCatalog catalog,
        IReadOnlyList<TranslationResult> results,
        string language)
    {
        if (results.Count == 0)
        {
            console.MarkupLine("[grey]No translations to apply[/]");
            return catalog;
        }

        foreach (var result in results)
        {
            var key = new POKey(
                result.OriginalUnit.MsgId,
                result.OriginalUnit.PluralId,
                result.OriginalUnit.Context
            );

            var existingEntry = catalog.Values.FirstOrDefault(e => e.Key.Equals(key));

            if (existingEntry != null) catalog.Remove(existingEntry);

            var newEntry = new POSingularEntry(key)
            {
                Translation = result.Translated
            };

            catalog.Add(newEntry);
            console.MarkupLine($"[green]✓[/] {result.OriginalUnit.MsgId.EscapeMarkup()}");
        }

        catalog.Language = language;
        catalog.Encoding = "UTF-8";

        if (catalog.Headers == null) catalog.Headers = new Dictionary<string, string>();

        catalog.Headers["Last-Modified"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        catalog.Headers["PO-GPT-Version"] = "1.0.0";

        return catalog;
    }
}