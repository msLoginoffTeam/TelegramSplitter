namespace BudgetSplitter.App.Authentication;

public static class TelegramAuthDefaults
{
    public const string Scheme = "Telegram";
    public const string InitDataHeaderName = "X-Telegram-Init-Data";
    public const string DevelopmentUserIdHeaderName = "X-Telegram-Dev-User-Id";
    public const string TelegramIdClaimType = "telegram_id";
}
