using System.ClientModel;
using Karambolo.PO;
using OpenAI;
using OpenAI.Chat;
using PO.GPT.Configs;
using Spectre.Console;
using Spectre.Console.Cli;
using VYaml.Serialization;

namespace PO.GPT.Commands;

public class TranslateCommand : AsyncCommand<TranslateCommand.Settings>
{
    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken ct)
    {
        var config = await LoadConfigAsync(settings.ConfigPath);
        if (settings.DryRun)
            AnsiConsole.Console.MarkupLine("[yellow]🔍 DRY RUN MODE - No API calls will be modified[/]\n");

        var potFiles = GetPotFiles(config, settings);
        if (potFiles.Length == 0) return 1;

        var translator = settings.DryRun ? new DryRunTranslator() : CreateAiTranslator(config.Llm);
        var parser = new POParser(new POParserSettings());
        var planner = new TranslationPlanner();
        var applier = new CatalogApplier();

        foreach (var potFile in potFiles)
            await ProcessPotFileAsync(planner, applier, parser, potFile, config, translator, ct);

        return 0;
    }

    private async Task<Config> LoadConfigAsync(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException($"Config file not found: {path}");
        await using var stream = File.OpenRead(path);
        var config = await YamlSerializer.DeserializeAsync<Config>(stream);
        return config ?? throw new InvalidDataException("Invalid configuration format.");
    }

    private string[] GetPotFiles(Config config, Settings settings)
    {
        var basePath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(settings.ConfigPath))!,
            config.Project.BasePath);
        AnsiConsole.Console.MarkupLine($"[grey]Searching in: {basePath}[/]");
        return Directory.GetFiles(basePath, config.Translate.InputPattern, SearchOption.AllDirectories);
    }

    private async Task ProcessPotFileAsync(
        ITranslationPlanner planner,
        ICatalogApplier applier,
        POParser parser,
        string potPath, Config config, ITranslator translator, CancellationToken ct)
    {
        AnsiConsole.Console.MarkupLine($"[grey]Processing: {Path.GetFileName(potPath)}[/]");

        var fileName = Path.GetFileNameWithoutExtension(potPath);

        var parseResult = parser.Parse(File.OpenRead(potPath));
        if (!parseResult.Success) throw new InvalidDataException($"Failed to parse: {fileName}.pot");

        foreach (var targetLang in config.Translate.TargetLanguages)
            await TranslateToLanguageAsync(planner, applier, parser, potPath, parseResult.Catalog, targetLang, config,
                translator, ct);
    }

    private async Task TranslateToLanguageAsync(
        ITranslationPlanner planner,
        ICatalogApplier applier,
        POParser parser,
        string potPath,
        POCatalog potCatalog,
        string lang,
        Config config,
        ITranslator translator,
        CancellationToken ct
    )
    {
        var outputPath = GetOutputPath(potPath, lang, config);
        var poCatalog = File.Exists(outputPath)
            ? parser.Parse(File.OpenRead(outputPath)).Catalog
            : new POCatalog();

        var merge = new PotPoMerger().Merge(potCatalog, poCatalog);
        var batches = planner.Plan(merge.Missing, config.Translate.BatchSize);

        var currentCatalog = poCatalog;

        for (var index = 0; index < batches.Count; index++)
        {
            var batch = batches[index];
            AnsiConsole.Console.MarkupLine($"[grey]Translating batch {index + 1} of {batches.Count}[/]");
            var results = await translator.TranslateAsync(batch.Units, lang, ct);
            currentCatalog = applier.Apply(currentCatalog, results, lang);

            // 这里可以增加一个保存逻辑，防止中途断电丢数据
            // SaveCatalog(currentCatalog, outputPath);
        }
    }

    private string GetOutputPath(string potPath, string lang, Config config)
    {
        var fileName = Path.GetFileNameWithoutExtension(potPath);
        var outputFileName = config.Translate.OutputPattern
            .Replace("{file}", fileName)
            .Replace("{lang}", lang);
        return Path.Combine(Path.GetDirectoryName(potPath)!, outputFileName);
    }

    private ITranslator CreateAiTranslator(LlmConfig llm)
    {
        return new AiTranslator(new ChatClient(llm.Model, new ApiKeyCredential(llm.ApiKey),
            new OpenAIClientOptions { Endpoint = new Uri(llm.ApiBase) }));
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<config>")] public string ConfigPath { get; set; } = "config.yaml";

        [CommandOption("-d|--dry-run")] public bool DryRun { get; set; }
    }
}