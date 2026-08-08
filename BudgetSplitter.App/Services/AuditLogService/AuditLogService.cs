using BudgetSplitter.Common.Dtos.Response;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace BudgetSplitter.App.Services.AuditLogService;

public sealed class AuditLogService(AppDbContext db) : IAuditLogService
{
    private const int DefaultPageSize = 30;
    private const int MaxPageSize = 100;

    public async Task<AuditLogPageResponseDto> GetGroupAuditLogAsync(Guid groupId, int offset, int take)
    {
        var normalizedOffset = Math.Max(offset, 0);
        var normalizedTake = take <= 0 ? DefaultPageSize : Math.Min(take, MaxPageSize);
        var rows = await db.AuditLogEntries
            .AsNoTracking()
            .Where(entry => entry.GroupId == groupId)
            .OrderByDescending(entry => entry.OccurredAtUtc)
            .ThenByDescending(entry => entry.Id)
            .Skip(normalizedOffset)
            .Take(normalizedTake + 1)
            .Select(entry => new AuditLogEntryResponseDto
            {
                Id = entry.Id,
                OccurredAtUtc = entry.OccurredAtUtc,
                ActorTelegramId = entry.ActorTelegramId,
                ActorDisplayName = entry.ActorDisplayName,
                ActorUsername = entry.ActorUsername,
                SubjectType = entry.SubjectType,
                Operation = entry.Operation,
                EntityKeyJson = entry.EntityKeyJson,
                OldValuesJson = entry.OldValuesJson,
                NewValuesJson = entry.NewValuesJson
            })
            .ToListAsync();

        var hasMore = rows.Count > normalizedTake;
        if (hasMore)
        {
            rows.RemoveAt(rows.Count - 1);
        }

        return new AuditLogPageResponseDto
        {
            Entries = rows,
            HasMore = hasMore,
            NextOffset = hasMore ? normalizedOffset + rows.Count : null
        };
    }
}
