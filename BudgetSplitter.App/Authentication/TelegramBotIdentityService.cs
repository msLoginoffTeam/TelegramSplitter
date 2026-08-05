using System.Text.Json;
using Microsoft.Extensions.Options;

namespace BudgetSplitter.App.Authentication;

public sealed class TelegramBotIdentityService
{
    private readonly HttpClient _httpClient;
    private readonly TelegramAuthOptions _options;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _cachedUsername;

    public TelegramBotIdentityService(HttpClient httpClient, IOptions<TelegramAuthOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<string> GetUsernameAsync(CancellationToken cancellationToken = default)
    {
        var configuredUsername = NormalizeUsername(_options.BotUsername);
        if (configuredUsername is not null)
        {
            return configuredUsername;
        }

        if (_cachedUsername is not null)
        {
            return _cachedUsername;
        }

        if (string.IsNullOrWhiteSpace(_options.BotToken))
        {
            throw new InvalidOperationException("TelegramAuth:BotToken is not configured.");
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedUsername is not null)
            {
                return _cachedUsername;
            }

            using var response = await _httpClient.GetAsync(
                $"https://api.telegram.org/bot{_options.BotToken}/getMe",
                cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
            var root = document.RootElement;
            var username = root.GetProperty("result").GetProperty("username").GetString();
            _cachedUsername = NormalizeUsername(username)
                ?? throw new InvalidOperationException("Telegram bot username was not returned by getMe.");
            return _cachedUsername;
        }
        finally
        {
            _lock.Release();
        }
    }

    private static string? NormalizeUsername(string? username)
    {
        var normalized = username?.Trim().TrimStart('@');
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
