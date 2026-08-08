using System.Text.Json;
using AuditLogLens.Changes;
using AuditLogLens.Writing;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace BudgetSplitter.App.Audit;

public sealed class AuditLogEntryMapper : IAuditEntryMapper<AuditLogEntry>
{
    public bool CanMap(DbContext dbContext) => dbContext is AppDbContext;

    public AuditLogEntry Map(AuditChange change, DbContext dbContext)
    {
        change.TryGetExtraValue<Guid>(AuditMetadataKeys.GroupId, out var groupId);
        change.TryGetExtraValue<long>(AuditMetadataKeys.ActorTelegramId, out var actorTelegramId);
        change.TryGetExtraValue<string>(AuditMetadataKeys.ActorDisplayName, out var actorDisplayName);
        change.TryGetExtraValue<string>(AuditMetadataKeys.ActorUsername, out var actorUsername);

        return new AuditLogEntry
        {
            GroupId = groupId == Guid.Empty ? null : groupId,
            ActorTelegramId = actorTelegramId == 0 ? null : actorTelegramId,
            ActorDisplayName = actorDisplayName,
            ActorUsername = actorUsername,
            SubjectType = change.TableName ?? change.EntityType.Name,
            Operation = change.State.ToString(),
            EntityKeyJson = JsonSerializer.Serialize(change.EntityId),
            OldValuesJson = SerializeOrNull(change.OldValues),
            NewValuesJson = SerializeOrNull(change.NewValues)
        };
    }

    private static string? SerializeOrNull(IReadOnlyDictionary<string, object?> values)
        => values.Count == 0 ? null : JsonSerializer.Serialize(values);
}
