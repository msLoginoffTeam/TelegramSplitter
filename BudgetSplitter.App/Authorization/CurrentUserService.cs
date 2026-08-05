using System.Security.Claims;
using BudgetSplitter.App.Authentication;
using BudgetSplitter.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace BudgetSplitter.App.Authorization;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor, AppDbContext db) : ICurrentUserService
{
    public async Task<User> GetRequiredUserAsync(CancellationToken cancellationToken = default)
    {
        var rawTelegramId = httpContextAccessor.HttpContext?.User.FindFirstValue(TelegramAuthDefaults.TelegramIdClaimType);
        if (!long.TryParse(rawTelegramId, out var telegramId) || telegramId <= 0)
        {
            throw new ForbiddenException("Telegram user identity is required.");
        }

        var user = await db.Users.SingleOrDefaultAsync(user => user.TelegramId == telegramId, cancellationToken);
        var principal = httpContextAccessor.HttpContext?.User;
        var hasDisplayName = principal?.Claims.Any(claim => claim.Type == TelegramAuthDefaults.TelegramDisplayNameClaimType) == true;
        var hasUsername = principal?.Claims.Any(claim => claim.Type == TelegramAuthDefaults.TelegramUsernameClaimType) == true;
        var displayName = principal?.FindFirstValue(TelegramAuthDefaults.TelegramDisplayNameClaimType);
        var username = principal?.FindFirstValue(TelegramAuthDefaults.TelegramUsernameClaimType);
        if (user is not null)
        {
            var changed = false;
            if (hasDisplayName && user.DisplayName != NullIfBlank(displayName))
            {
                user.DisplayName = NullIfBlank(displayName);
                changed = true;
            }
            if (hasUsername && user.Username != NullIfBlank(username))
            {
                user.Username = NullIfBlank(username);
                changed = true;
            }
            if (changed) await db.SaveChangesAsync(cancellationToken);
            return user;
        }

        user = new User
        {
            TelegramId = telegramId,
            DisplayName = NullIfBlank(displayName),
            Username = NullIfBlank(username)
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return user;
    }

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
