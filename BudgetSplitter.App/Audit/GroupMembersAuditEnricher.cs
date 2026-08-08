using AuditLogLens.Enrichment;
using AuditLogLens.Enrichment.Extensions;
using Persistence;

namespace BudgetSplitter.App.Audit;

/// <summary>
/// Represents changes in the UserGroups join table as a readable Members collection on Group.
/// </summary>
public sealed class GroupMembersAuditEnricher : AuditEntityEnricherBase
{
    public override bool CanHandle(Type entityType) => entityType == typeof(Group);

    public override void Configure(IAuditEnrichmentPlanBuilder builder)
    {
        builder.Collection<Group, UserGroup, User>(
            joinParentKey: membership => membership.GroupId,
            joinItemKey: membership => membership.UserId,
            fieldName: "Members",
            itemValueSelector: user => FormatUser(user));
    }

    private static string FormatUser(User user)
        => !string.IsNullOrWhiteSpace(user.DisplayName)
            ? user.DisplayName
            : !string.IsNullOrWhiteSpace(user.Username)
                ? $"@{user.Username}"
                : user.TelegramId.ToString();
}
