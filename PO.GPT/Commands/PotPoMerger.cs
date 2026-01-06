using Karambolo.PO;

namespace PO.GPT.Commands;

public class PotPoMerger : IPotPoMerger
{
    public MergeResult Merge(POCatalog pot, POCatalog existingPo)
    {
        var missing = pot
            .Values
            .Where(u => !existingPo.Values.Contains(u))
            .ToList();
        return new MergeResult(pot, missing.Select(x => new TranslationUnit(
                x.Key.Id,
                x.Key.PluralId,
                x.Key.ContextId
            )).ToList()
        );
    }
}