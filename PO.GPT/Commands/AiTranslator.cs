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
            console.MarkupLine($"[cyan]📝 Custom instruction:[/] {userPrompt.EscapeMarkup()}");

        await console.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("blue"))
            .StartAsync("[blue]🤔 Thinking...[/]", async ctx => { await Task.Delay(100, ct); });

        var systemPrompt = BuildSystemPrompt(targetLanguage, userPrompt);
        var userMessage = BuildPrompt(batch, targetLanguage);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userMessage)
        };

        console.MarkupLine("[grey]→ Sending request to AI...[/]");

        var options = new ChatCompletionOptions
        {
            Temperature = 0.3f,
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
                                                              "description": "The index from the input array"
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

        console.MarkupLine(
            $"[grey]✓ Received response ({usage.InputTokenCount} in + {usage.OutputTokenCount} out = {usage.TotalTokenCount} tokens)[/]");

        return ParseTranslations(batch, response.Value.Content[0].Text);
    }

    private string BuildSystemPrompt(string targetLanguage, string userPrompt)
    {
        return $@"You are a professional translator specializing in {targetLanguage}.

TRANSLATION RULES:
1. Translate text accurately while preserving the original meaning and tone
2. Maintain consistency in terminology across all translations in the batch
3. Preserve ALL formatting exactly: placeholders ({{0}}, {{1}}, %s, %d), line breaks (\r\n, \n), special characters
4. Keep proper nouns in their original form unless they have established translations
5. If context is provided, use it to disambiguate and improve translation accuracy
6. For empty strings, return empty translations
7. Preserve leading/trailing whitespace if present

{(!string.IsNullOrEmpty(userPrompt) ? $@"ADDITIONAL REQUIREMENTS:
{userPrompt}

" : "")}CRITICAL: Your response MUST match the JSON schema exactly. The response format is strictly enforced.";
    }

    private string BuildPrompt(IReadOnlyList<TranslationUnit> batch, string targetLanguage)
    {
        var sourceTexts = batch.Select((unit, index) => new
        {
            index,
            text = unit.MsgId,
            context = !string.IsNullOrWhiteSpace(unit.Context) ? unit.Context : null
        }).ToList();

        var json = JsonSerializer.Serialize(new { sourceTexts }, JsonOptions);

        return
            $@"Translate the following {sourceTexts.Count} text {(sourceTexts.Count == 1 ? "segment" : "segments")} to {targetLanguage}.

INPUT DATA:
{json}

INSTRUCTIONS:
- Each item has ""index"" (for matching), ""text"" (to translate), and optional ""context"" (for disambiguation)
- You MUST return ALL {sourceTexts.Count} translations
- Use the exact ""index"" from input in your output
- Return format: {{""translations"": [{{""index"": 0, ""translation"": ""text""}}]}}";
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
                console.MarkupLine("[red]✗ Failed to parse AI response: null response[/]");
                console.MarkupLine($"[grey]Raw response: {jsonResponse[..Math.Min(500, jsonResponse.Length)]}[/]");
                return [];
            }

            if (response.Translations.Count != batch.Count)
                console.MarkupLine(
                    $"[yellow]⚠ Warning: Expected {batch.Count} translations, got {response.Translations.Count}[/]");

            var results = new List<TranslationUnit>();
            var processedIndices = new HashSet<int>();

            foreach (var item in response.Translations)
                if (item.Index >= 0 && item.Index < batch.Count)
                {
                    var original = batch[item.Index];
                    results.Add(original.WithTranslation(item.Translation));
                    processedIndices.Add(item.Index);
                }
                else
                {
                    console.MarkupLine($"[yellow]⚠ Invalid index {item.Index} (valid range: 0-{batch.Count - 1})[/]");
                }

            var missingIndices = Enumerable.Range(0, batch.Count)
                .Where(i => !processedIndices.Contains(i))
                .ToList();

            if (missingIndices.Any())
            {
                console.MarkupLine(
                    $"[yellow]⚠ Missing translations for indices: {string.Join(", ", missingIndices)}[/]");

                foreach (var idx in missingIndices)
                {
                    var original = batch[idx];
                    results.Add(original.WithTranslation(original.MsgId));
                    console.MarkupLine($"[grey]  → Index {idx}: kept original text[/]");
                }
            }

            return results.ToList();
        }
        catch (JsonException ex)
        {
            console.MarkupLine($"[red]✗ JSON parsing error: {ex.Message}[/]");
            console.MarkupLine($"[grey]Response preview: {jsonResponse[..Math.Min(200, jsonResponse.Length)]}...[/]");
            return [];
        }
    }
}