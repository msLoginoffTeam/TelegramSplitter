namespace BudgetSplitter.App.Authentication;

public sealed class TelegramAuthOptions
{
    public const string SectionName = "TelegramAuth";

    public string? BotToken { get; set; }

    public string? BotUsername { get; set; }

    public int MaxAuthAgeSeconds { get; set; } = 86_400;

    public int InviteExpirationHours { get; set; } = 168;
}
