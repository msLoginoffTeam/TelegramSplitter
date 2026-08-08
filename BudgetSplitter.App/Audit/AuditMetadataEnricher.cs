using System.Security.Claims;
using AuditLogLens.Changes;
using AuditLogLens.Enrichment;
using AuditLogLens.Enrichment.Context;
using BudgetSplitter.App.Authentication;
using Persistence;

namespace BudgetSplitter.App.Audit;

public sealed class AuditMetadataEnricher(IHttpContextAccessor httpContextAccessor) : AuditEntityEnricherBase
{
    public override bool CanHandle(Type entityType) => true;

    protected override Task AfterMergeChangeAsync(
        AuditEnrichmentContext context,
        AuditChange change,
        CancellationToken cancellationToken = default)
    {
        SetGroupId(change);
        SetActor(change);
        return Task.CompletedTask;
    }

    private static void SetGroupId(AuditChange change)
    {
        if (change.ExtraValues.ContainsKey(AuditMetadataKeys.GroupId))
        {
            return;
        }

        var groupId = change.Entity switch
        {
            Group group => group.Id,
            UserGroup membership => membership.GroupId,
            Expense expense => expense.GroupId,
            Payment payment => payment.GroupId,
            GroupInvite invite => invite.GroupId,
            _ => (Guid?)null
        };

        if (groupId is null && change.EntityType == typeof(Group) && TryGetGuid(change.EntityId, out var syntheticGroupId))
        {
            groupId = syntheticGroupId;
        }

        if (groupId is { } value && value != Guid.Empty)
        {
            change.SetExtraValue(AuditMetadataKeys.GroupId, value);
        }
    }

    private static bool TryGetGuid(object? value, out Guid result)
        => value is Guid guid
            ? (result = guid) != Guid.Empty
            : Guid.TryParse(value?.ToString(), out result);

    private void SetActor(AuditChange change)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        if (!change.ExtraValues.ContainsKey(AuditMetadataKeys.ActorTelegramId) &&
            long.TryParse(user.FindFirstValue(TelegramAuthDefaults.TelegramIdClaimType), out var telegramId))
        {
            change.SetExtraValue(AuditMetadataKeys.ActorTelegramId, telegramId);
        }

        if (!change.ExtraValues.ContainsKey(AuditMetadataKeys.ActorDisplayName))
        {
            var displayName = user.FindFirstValue(TelegramAuthDefaults.TelegramDisplayNameClaimType);
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                change.SetExtraValue(AuditMetadataKeys.ActorDisplayName, displayName);
            }
        }

        if (!change.ExtraValues.ContainsKey(AuditMetadataKeys.ActorUsername))
        {
            var username = user.FindFirstValue(TelegramAuthDefaults.TelegramUsernameClaimType);
            if (!string.IsNullOrWhiteSpace(username))
            {
                change.SetExtraValue(AuditMetadataKeys.ActorUsername, username);
            }
        }
    }
}
