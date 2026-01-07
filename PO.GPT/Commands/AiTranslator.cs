using OpenAI.Chat;
using Spectre.Console;

namespace PO.GPT.Commands;

public class AiTranslator(ChatClient client, TokenCounter tokenCounter, IAnsiConsole console)
    : ITranslator
{
    public async Task<IReadOnlyList<TranslationResult>> TranslateAsync(
        IReadOnlyList<TranslationUnit> batch,
        string targetLanguage,
        CancellationToken ct)
    {
        await console.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("blue"))
            .StartAsync("[blue]🤔 Thinking...[/]", async ctx => { await Task.Delay(100, ct); });

        var prompt = BuildPrompt(batch, targetLanguage);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(
                "You are a professional translator. Translate the given text to the target language accurately."),
            new UserChatMessage(prompt)
        };

        console.MarkupLine("[grey]→ Sending request to AI...[/]");

        var response = await client.CompleteChatAsync(messages, cancellationToken: ct);

        var usage = response.Value.Usage;
        tokenCounter.AddUsage(usage.InputTokenCount, usage.OutputTokenCount);

        console.MarkupLine($"[grey]✓ Received response ({usage.TotalTokenCount} tokens)[/]");

        return ParseTranslations(batch, response.Value.Content[0].Text);
    }

    private string BuildPrompt(IReadOnlyList<TranslationUnit> batch, string targetLanguage)
    {
        var lines = batch.Select((unit, index) =>
            $"{index}||{unit.MsgId}");

        return $"Translate to {targetLanguage}. Format: index||translation\n\n" +
               string.Join("\n", lines);
    }

    private IReadOnlyList<TranslationResult> ParseTranslations(
        IReadOnlyList<TranslationUnit> batch,
        string response)
    {
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var results = new List<TranslationResult>();

        foreach (var line in lines)
        {
            var parts = line.Split("||", 2);
            if (parts.Length == 2 && int.TryParse(parts[0], out var index) && index < batch.Count)
                results.Add(new TranslationResult(batch[index], parts[1].Trim()));
        }

        return results;
    }
}