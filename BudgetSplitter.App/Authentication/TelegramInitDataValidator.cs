using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace BudgetSplitter.App.Authentication;

public sealed class TelegramInitDataValidator
{
    private static readonly TimeSpan AllowedClockSkew = TimeSpan.FromMinutes(5);

    public bool TryValidate(
        string initData,
        TelegramAuthOptions options,
        out long telegramUserId,
        out string failureReason)
    {
        telegramUserId = default;
        failureReason = "Invalid Telegram init data.";

        if (string.IsNullOrWhiteSpace(options.BotToken))
        {
            failureReason = "Telegram authentication is not configured.";
            return false;
        }

        if (options.MaxAuthAgeSeconds <= 0)
        {
            failureReason = "Telegram authentication is misconfigured.";
            return false;
        }

        var parsed = QueryHelpers.ParseQuery(initData);
        if (!parsed.TryGetValue("hash", out var hashValues) || hashValues.Count != 1 ||
            !TryDecodeHex(hashValues[0], out var providedHash))
        {
            return false;
        }

        var dataCheckFields = new List<KeyValuePair<string, string>>();
        foreach (var (key, values) in parsed)
        {
            if (string.Equals(key, "hash", StringComparison.Ordinal))
            {
                continue;
            }

            if (values.Count != 1)
            {
                return false;
            }

            dataCheckFields.Add(new KeyValuePair<string, string>(key, values[0]!));
        }

        var dataCheckString = string.Join(
            "\n",
            dataCheckFields
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => $"{pair.Key}={pair.Value}"));

        var secretKey = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes("WebAppData"),
            Encoding.UTF8.GetBytes(options.BotToken));
        var calculatedHash = HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(dataCheckString));

        if (!CryptographicOperations.FixedTimeEquals(providedHash, calculatedHash))
        {
            return false;
        }

        if (!parsed.TryGetValue("auth_date", out var authDateValues) || authDateValues.Count != 1 ||
            !long.TryParse(authDateValues[0], out var authDateUnixSeconds))
        {
            return false;
        }

        DateTimeOffset authenticatedAt;
        try
        {
            authenticatedAt = DateTimeOffset.FromUnixTimeSeconds(authDateUnixSeconds);
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        if (authenticatedAt > now.Add(AllowedClockSkew) || now - authenticatedAt > TimeSpan.FromSeconds(options.MaxAuthAgeSeconds))
        {
            failureReason = "Telegram init data has expired.";
            return false;
        }

        if (!parsed.TryGetValue("user", out var userValues) || userValues.Count != 1 ||
            !TryGetTelegramUserId(userValues[0], out telegramUserId))
        {
            return false;
        }

        return true;
    }

    private static bool TryDecodeHex(string? value, out byte[] bytes)
    {
        try
        {
            bytes = Convert.FromHexString(value ?? string.Empty);
            return bytes.Length == 32;
        }
        catch (FormatException)
        {
            bytes = [];
            return false;
        }
    }

    private static bool TryGetTelegramUserId(string? userJson, out long telegramUserId)
    {
        telegramUserId = default;

        try
        {
            using var document = JsonDocument.Parse(userJson ?? string.Empty);
            return document.RootElement.ValueKind == JsonValueKind.Object &&
                   document.RootElement.TryGetProperty("id", out var id) &&
                   id.TryGetInt64(out telegramUserId) &&
                   telegramUserId > 0;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
