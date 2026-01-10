using Karambolo.PO;
using Spectre.Console;

namespace PO.GPT.Commands;

public class CatalogApplier(IAnsiConsole console)
{
    public POCatalog Apply(
        POCatalog catalog,
        IReadOnlyList<TranslationUnit> translatedUnits,
        string language)
    {
        if (translatedUnits.Count == 0)
        {
            console.MarkupLine("[grey]No translations to apply[/]");
            return catalog;
        }

        foreach (var unit in translatedUnits)
        {
            var key = new POKey(unit.MsgId, unit.PluralId, unit.Context);
            var existingEntry = catalog.Values.FirstOrDefault(e => e.Key.Equals(key));

            if (existingEntry != null)
                catalog.Remove(existingEntry);

            var newEntry = new POSingularEntry(key)
            {
                Translation = unit.ExistingTranslation
            };

            catalog.Add(newEntry);
            console.MarkupLine($"[green]✓[/] {unit.MsgId.EscapeMarkup()}");
        }

        catalog.Language = language;
        catalog.Encoding = "UTF-8";

        catalog.Headers ??= new Dictionary<string, string>();
        catalog.Headers["Last-Modified"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        catalog.Headers["PO-GPT-Version"] = "1.0.0";

        return catalog;
    }
}