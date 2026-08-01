namespace BudgetSplitter.App.Authentication;

public sealed class TelegramAuthOptions
{
    public const string SectionName = "TelegramAuth";

    public string? BotToken { get; set; }

    public int MaxAuthAgeSeconds { get; set; } = 86_400;
}
