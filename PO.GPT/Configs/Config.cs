using VYaml.Annotations;

namespace PO.GPT.Configs;

[YamlObject]
public partial record Config(
    TranslateConfig Translate,
    ProjectConfig Project,
    LlmConfig Llm
);

[YamlObject]
public partial record TranslateConfig(
    string[] SourceLanguages,
    string[] TargetLanguages,
    string InputPattern,
    string OutputPattern,
    bool SkipTranslated = true,
    int BatchSize = 20
);

[YamlObject]
public partial record ProjectConfig(
    string Name,
    string Context,
    string BasePath
);

[YamlObject]
public partial record LlmConfig(
    string Model,
    string ApiKey,
    string ApiBase
);