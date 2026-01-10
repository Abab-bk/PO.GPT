using System.Text.Json;
using System.Text.Json.Serialization;
using OpenAI.Chat;
using Spectre.Console;

namespace PO.GPT.Commands;

public class TranslationResponse
{
    [JsonPropertyName("translations")] public List<TranslationItem> Translations { get; set; } = new();
}

public class TranslationItem
{
    [JsonPropertyName("index")] public int Index { get; set; }

    [JsonPropertyName("translation")] public string Translation { get; set; } = string.Empty;
}

public class AiTranslator(ChatClient client, TokenCounter tokenCounter, IAnsiConsole console)
    : ITranslator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task<IReadOnlyList<TranslationUnit>> TranslateAsync(
        IReadOnlyList<TranslationUnit> batch,
        string targetLanguage,
        string userPrompt,
        CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(userPrompt))
            console.MarkupLine($"Your prompt: {userPrompt}");

        await console.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("blue"))
            .StartAsync("[blue]🤔 Thinking...[/]", async ctx => { await Task.Delay(100, ct); });

        var prompt = BuildPrompt(batch, targetLanguage);
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(
                "You are a professional translator. Translate the given text to the target language accurately. " +
                "Pay attention to the context provided for each text. " +
                "You must respond with a JSON object containing a 'translations' array."),
            new UserChatMessage(prompt)
        };

        if (!string.IsNullOrEmpty(userPrompt))
            messages.Add(new UserChatMessage(userPrompt));

        console.MarkupLine("[grey]→ Sending request to AI...[/]");

        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                "translation_response",
                BinaryData.FromString("""
                                      {
                                          "type": "object",
                                          "properties": {
                                              "translations": {
                                                  "type": "array",
                                                  "items": {
                                                      "type": "object",
                                                      "properties": {
                                                          "index": {
                                                              "type": "integer",
                                                              "description": "The index of the translation unit"
                                                          },
                                                          "translation": {
                                                              "type": "string",
                                                              "description": "The translated text"
                                                          }
                                                      },
                                                      "required": ["index", "translation"],
                                                      "additionalProperties": false
                                                  }
                                              }
                                          },
                                          "required": ["translations"],
                                          "additionalProperties": false
                                      }
                                      """),
                jsonSchemaIsStrict: true
            )
        };

        var response = await client.CompleteChatAsync(messages, options, ct);

        var usage = response.Value.Usage;
        tokenCounter.AddUsage(usage.InputTokenCount, usage.OutputTokenCount);

        console.MarkupLine($"[grey]✓ Received response ({usage.TotalTokenCount} tokens)[/]");

        return ParseTranslations(batch, response.Value.Content[0].Text);
    }

    private string BuildPrompt(IReadOnlyList<TranslationUnit> batch, string targetLanguage)
    {
        var items = batch.Select((unit, index) => new
        {
            index,
            text = unit.MsgId,
            context = unit.Context // 添加 context 字段
        });

        var json = JsonSerializer.Serialize(new { items }, JsonOptions);

        return $"""
                Translate the following texts to {targetLanguage}.

                Each item may have a "context" field that provides additional information about the usage of the text.
                Use this context to provide more accurate translations.

                Input:
                {json}

                Return a JSON object with a "translations" array where each item has:
                - "index": the same index from input
                - "translation": the translated text
                """;
    }

    private IReadOnlyList<TranslationUnit> ParseTranslations(
        IReadOnlyList<TranslationUnit> batch,
        string jsonResponse)
    {
        try
        {
            var response = JsonSerializer.Deserialize<TranslationResponse>(
                jsonResponse,
                JsonOptions);

            if (response?.Translations == null)
            {
                console.MarkupLine("[red]✗ Failed to parse AI response[/]");
                return [];
            }

            var results = new List<TranslationUnit>();

            foreach (var item in response.Translations)
                if (item.Index >= 0 && item.Index < batch.Count)
                {
                    var original = batch[item.Index];
                    results.Add(original.WithTranslation(item.Translation));
                }
                else
                {
                    console.MarkupLine($"[yellow]⚠ Invalid index {item.Index} in response[/]");
                }

            return results;
        }
        catch (JsonException ex)
        {
            console.MarkupLine($"[red]✗ JSON parsing error: {ex.Message}[/]");
            return [];
        }
    }
}