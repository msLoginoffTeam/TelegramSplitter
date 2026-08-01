namespace BudgetSplitter.App.Authentication;

public sealed class TelegramAuthOptions
{
    public const string SectionName = "TelegramAuth";

    public string? BotToken { get; init; }

    public int MaxAuthAgeSeconds { get; init; } = 86_400;
}
