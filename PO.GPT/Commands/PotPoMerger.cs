using Karambolo.PO;
using Spectre.Console;

namespace PO.GPT.Commands;

public class PotPoMerger(IAnsiConsole console)
{
    public IReadOnlyList<TranslationUnit> Merge(
        POCatalog pot,
        POCatalog existingPo,
        bool skipTranslated)
    {
        var unitsToTranslate = new List<TranslationUnit>();
        var stats = new MergeStats();

        console.MarkupLine("[bold blue]Merging POT with existing PO...[/]");

        foreach (var potEntry in pot.Values)
        {
            var key = potEntry.Key;
            var existingTranslation = existingPo.GetTranslation(key);
            var hasTranslation = !string.IsNullOrEmpty(existingTranslation);

            if (skipTranslated && hasTranslation)
            {
                stats.Skipped++;
                continue;
            }

            var unit = new TranslationUnit(
                key.Id,
                key.ContextId,
                key.PluralId,
                existingTranslation ?? string.Empty
            );

            unitsToTranslate.Add(unit);

            if (hasTranslation)
            {
                stats.Existing++;
            }
            else
            {
                stats.New++;
                console.MarkupLine($"  [yellow]+[/] {key.Id.EscapeMarkup()}");
            }
        }

        RenderSummary(stats, unitsToTranslate.Count);

        return unitsToTranslate;
    }

    private void RenderSummary(MergeStats stats, int total)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn("[bold]Status[/]")
            .AddColumn("[bold]Count[/]", c => c.RightAligned());

        table.AddRow("[yellow]New (needs translation)[/]", stats.New.ToString());
        table.AddRow("[blue]Existing (will re-translate)[/]", stats.Existing.ToString());

        if (stats.Skipped > 0)
            table.AddRow("[grey]Skipped (already translated)[/]", stats.Skipped.ToString());

        table.AddRow("[bold white]Total to process[/]", total.ToString());

        console.WriteLine();
        console.Write(table);
    }

    private class MergeStats
    {
        public int New { get; set; }
        public int Existing { get; set; }
        public int Skipped { get; set; }
    }
}