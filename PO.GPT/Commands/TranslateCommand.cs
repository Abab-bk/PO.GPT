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
    private readonly POParser _parser = new(new POParserSettings());
    private readonly POGenerator _poGenerator = new();
    private readonly TokenCounter _tokenCounter = new();

    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken ct)
    {
        var config = await LoadConfigAsync(settings.ConfigPath);

        RenderHeader(settings.DryRun);

        var potFiles = DiscoverPotFiles(config, settings);
        if (potFiles.Length == 0)
        {
            AnsiConsole.Console.MarkupLine("[red]No POT files found![/]");
            return 1;
        }

        var translator = CreateTranslator(settings.DryRun, config.Llm);
        var planner = new TranslationPlanner();
        var merger = new PotPoMerger(AnsiConsole.Console);
        var applier = new CatalogApplier(AnsiConsole.Console);

        foreach (var potFile in potFiles)
            await ProcessPotFileAsync(
                potFile,
                config,
                translator,
                planner,
                merger,
                applier,
                ct);

        AnsiConsole.Console.MarkupLine("\n[green]✓ All translations completed[/]");

        _tokenCounter.RenderSummary(AnsiConsole.Console, config.Llm.Model);

        return 0;
    }

    private void RenderHeader(bool dryRun)
    {
        var panel = new Panel(
            dryRun
                ? "[yellow]⚠ DRY RUN MODE[/]\nSimulated translations only\nToken usage will be estimated"
                : "[green]🚀 LIVE MODE[/]\nReal API calls will be made\nTokens will be charged"
        )
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(dryRun ? Color.Yellow : Color.Green)
        };

        AnsiConsole.Console.Write(panel);
        AnsiConsole.Console.WriteLine();
    }

    private async Task<Config> LoadConfigAsync(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException($"Config not found: {path}");

        await using var stream = File.OpenRead(path);
        return await YamlSerializer.DeserializeAsync<Config>(stream)
               ?? throw new InvalidDataException("Invalid config format");
    }

    private string[] DiscoverPotFiles(Config config, Settings settings)
    {
        var basePath = Path.Combine(
            Path.GetDirectoryName(Path.GetFullPath(settings.ConfigPath))!,
            config.Project.BasePath);

        AnsiConsole.Console.MarkupLine($"[grey]Scanning: {basePath}[/]");

        return Directory.GetFiles(
            basePath,
            config.Translate.InputPattern,
            SearchOption.AllDirectories);
    }

    private async Task ProcessPotFileAsync(
        string potPath,
        Config config,
        ITranslator translator,
        ITranslationPlanner planner,
        IPotPoMerger merger,
        ICatalogApplier applier,
        CancellationToken ct)
    {
        var fileName = Path.GetFileNameWithoutExtension(potPath);
        AnsiConsole.Console.MarkupLine($"\n[bold cyan]Processing: {fileName}.pot[/]");

        var potCatalog = await ParseCatalogAsync(potPath);

        foreach (var targetLang in config.Translate.TargetLanguages)
            await TranslateLanguageAsync(
                potPath,
                potCatalog,
                targetLang,
                config,
                translator,
                planner,
                merger,
                applier,
                ct);
    }

    private async Task TranslateLanguageAsync(
        string potPath,
        POCatalog potCatalog,
        string lang,
        Config config,
        ITranslator translator,
        ITranslationPlanner planner,
        IPotPoMerger merger,
        ICatalogApplier applier,
        CancellationToken ct)
    {
        AnsiConsole.Console.MarkupLine($"\n[bold]→ Target language: {lang}[/]");

        var outputPath = BuildOutputPath(potPath, lang, config);
        var existingPo = await LoadOrCreatePoAsync(outputPath);

        var mergeResult = merger.Merge(
            potCatalog,
            existingPo,
            config.Translate.SkipTranslated);

        if (mergeResult.Missing.Count == 0)
        {
            AnsiConsole.Console.MarkupLine("[grey]Nothing to translate[/]");
            return;
        }

        var batches = planner.Plan(mergeResult.Missing, config.Translate.BatchSize);
        var updatedCatalog = mergeResult.BaseCatalog;

        AnsiConsole.Console.MarkupLine($"[grey]Processing {batches.Count} batch(es)...[/]");

        for (var i = 0; i < batches.Count; i++)
        {
            AnsiConsole.Console.MarkupLine($"\n[bold]Batch {i + 1}/{batches.Count}[/]");

            var results = await translator.TranslateAsync(
                batches[i].Units,
                lang,
                ct);

            updatedCatalog = applier.Apply(updatedCatalog, results, lang);
        }

        await SaveCatalogAsync(outputPath, updatedCatalog);
        AnsiConsole.Console.MarkupLine($"[green]✓ Saved to: {Path.GetFileName(outputPath)}[/]");
    }

    private async Task<POCatalog> ParseCatalogAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        var result = _parser.Parse(stream);

        if (!result.Success) throw new InvalidDataException($"Failed to parse: {path}");

        return result.Catalog;
    }

    private async Task<POCatalog> LoadOrCreatePoAsync(string path)
    {
        if (!File.Exists(path)) return new POCatalog();

        await using var stream = File.OpenRead(path);
        return _parser.Parse(stream).Catalog;
    }

    private async Task SaveCatalogAsync(string path, POCatalog catalog)
    {
        await using var stream = File.Open(path, FileMode.Create);
        _poGenerator.Generate(stream, catalog);
    }

    private string BuildOutputPath(string potPath, string lang, Config config)
    {
        var fileName = Path.GetFileNameWithoutExtension(potPath);
        var outputFileName = config.Translate.OutputPattern
            .Replace("{file}", fileName)
            .Replace("{lang}", lang);

        return Path.Combine(Path.GetDirectoryName(potPath)!, outputFileName);
    }

    private ITranslator CreateTranslator(bool dryRun, LlmConfig llm)
    {
        if (dryRun) return new DryRunTranslator(_tokenCounter, AnsiConsole.Console, llm.Model);

        var client = new ChatClient(
            llm.Model,
            new ApiKeyCredential(llm.ApiKey),
            new OpenAIClientOptions { Endpoint = new Uri(llm.ApiBase) });

        return new AiTranslator(client, _tokenCounter, AnsiConsole.Console);
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<config>")] public string ConfigPath { get; set; } = "config.yaml";

        [CommandOption("-d|--dry-run")] public bool DryRun { get; set; }
    }
}