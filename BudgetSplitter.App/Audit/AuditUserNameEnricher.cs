using AuditLogLens.Changes;
using AuditLogLens.Enrichment;
using AuditLogLens.Enrichment.Context;
using AuditLogLens.Enrichment.Extensions;
using Persistence;

namespace BudgetSplitter.App.Audit;

public sealed class AuditUserNameEnricher : AuditEntityEnricherBase
{
    private static readonly HashSet<string> UserIdPropertyNames =
    [
        nameof(Expense.PayerId),
        nameof(Expense.CreatedByUserId),
        nameof(Payment.FromUserId),
        nameof(Payment.ToUserId),
        nameof(Payment.CreatedByUserId),
        nameof(Group.CreatedById),
        nameof(Group.OwnerId),
        nameof(UserGroup.UserId),
        nameof(GroupMemberPermission.UserId),
        "MemberUserId"
    ];

    public override bool CanHandle(Type entityType) => true;

    public override void Configure(IAuditEnrichmentPlanBuilder builder)
    {
        builder.Lookup<User, Guid>(user => user.Id, ExtractUserIds);
    }

    protected override Task AfterMergeChangeAsync(
        AuditEnrichmentContext context,
        AuditChange change,
        CancellationToken cancellationToken = default)
    {
        var namesByUserId = context
            .GetLoaded<User>(nameof(User.Id))
            .ToDictionary(user => user.Id, FormatUser);

        AddNames(change.OldValues, change.OldValues, namesByUserId);
        AddNames(change.NewValues, change.NewValues, namesByUserId);
        return Task.CompletedTask;
    }

    private static IEnumerable<object?> ExtractUserIds(AuditChange change)
    {
        foreach (var values in new[] { change.OldValues, change.NewValues })
        {
            foreach (var (name, value) in values)
            {
                if (UserIdPropertyNames.Contains(name) && value is not null)
                {
                    yield return value;
                }
            }
        }
    }

    private static void AddNames(
        Dictionary<string, object?> source,
        Dictionary<string, object?> target,
        IReadOnlyDictionary<Guid, string> namesByUserId)
    {
        foreach (var (propertyName, value) in source.ToArray())
        {
            if (!UserIdPropertyNames.Contains(propertyName) || !TryGetGuid(value, out var userId))
            {
                continue;
            }

            var nameProperty = propertyName[..^2] + "Name";
            if (!target.ContainsKey(nameProperty) && namesByUserId.TryGetValue(userId, out var name))
            {
                target[nameProperty] = name;
            }
        }
    }

    private static bool TryGetGuid(object? value, out Guid result)
        => value is Guid guid
            ? (result = guid) != Guid.Empty
            : Guid.TryParse(value?.ToString(), out result);

    private static string FormatUser(User user)
        => !string.IsNullOrWhiteSpace(user.DisplayName)
            ? user.DisplayName
            : !string.IsNullOrWhiteSpace(user.Username)
                ? $"@{user.Username}"
                : user.TelegramId.ToString();
}
