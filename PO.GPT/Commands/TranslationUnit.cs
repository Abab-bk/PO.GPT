namespace PO.GPT.Commands;

public record TranslationUnit(
    string MsgId,
    string? PluralId,
    string? Context
);