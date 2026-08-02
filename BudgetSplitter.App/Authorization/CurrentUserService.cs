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
        if (user is not null) return user;

        user = new User { TelegramId = telegramId };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return user;
    }
}
