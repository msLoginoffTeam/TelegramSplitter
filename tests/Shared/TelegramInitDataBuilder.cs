using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BudgetSplitter.App.Authentication;

namespace BudgetSplitter.Tests.Shared;

internal static class TelegramInitDataBuilder
{
    public const string BotToken = "telegram-auth-test-token";

    public static string Create(
        long telegramUserId = 123_456_789,
        DateTimeOffset? authenticatedAt = null,
        string? queryId = null)
    {
        var fields = new Dictionary<string, string>
        {
            ["auth_date"] = (authenticatedAt ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds().ToString(),
            ["query_id"] = queryId ?? "AAEAAAE",
            ["user"] = JsonSerializer.Serialize(new { id = telegramUserId, first_name = "Test" })
        };

        var dataCheckString = string.Join(
            "\n",
            fields
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => $"{pair.Key}={pair.Value}"));
        var secretKey = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes("WebAppData"),
            Encoding.UTF8.GetBytes(BotToken));
        var hash = Convert.ToHexString(HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(dataCheckString)))
            .ToLowerInvariant();

        fields["hash"] = hash;
        return string.Join(
            "&",
            fields.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
    }

    public static TelegramAuthOptions CreateOptions(int maxAuthAgeSeconds = 3_600) => new()
    {
        BotToken = BotToken,
        MaxAuthAgeSeconds = maxAuthAgeSeconds
    };
}
