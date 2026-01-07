namespace PO.GPT.Commands;

public static class TokenEstimator
{
    public static int EstimateTokens(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;

        var charCount = text.Length;
        var wordCount = text.Split(
            new[] { ' ', '\n', '\r', '\t' },
            StringSplitOptions.RemoveEmptyEntries
        ).Length;

        return (int)Math.Ceiling((charCount + wordCount) / 4.0);
    }

    public static int EstimateBatchTokens(IReadOnlyList<TranslationUnit> units, string language)
    {
        var totalChars = units.Sum(u => u.MsgId.Length);
        var systemPrompt = $"You are a professional translator. Translate to {language}.";
        var overhead = systemPrompt.Length + 100;

        return EstimateTokens(new string('x', totalChars + overhead));
    }
}