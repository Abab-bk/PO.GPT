namespace PO.GPT.Commands;

public record TranslationUnit(
    string MsgId,
    string Translated,
    string? PluralId,
    string? Context
);