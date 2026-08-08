using AuditLogLens.Changes;
using AuditLogLens.Enrichment;
using AuditLogLens.Enrichment.Context;
using AuditLogLens.Enrichment.Extensions;
using Persistence;

namespace BudgetSplitter.App.Audit;

public sealed class ExpenseShareGroupAuditEnricher : AuditEntityEnricherBase
{
    public override bool CanHandle(Type entityType) => entityType == typeof(ExpenseShare);

    public override void Configure(IAuditEnrichmentPlanBuilder builder)
    {
        builder.Lookup<Expense, Guid>(expense => expense.Id, ExtractExpenseIds);
    }

    protected override Task AfterMergeChangeAsync(
        AuditEnrichmentContext context,
        AuditChange change,
        CancellationToken cancellationToken = default)
    {
        if (change.ExtraValues.ContainsKey(AuditMetadataKeys.GroupId))
        {
            return Task.CompletedTask;
        }

        var groupsByExpenseId = context
            .GetLoaded<Expense>(nameof(Expense.Id))
            .ToDictionary(expense => expense.Id, expense => expense.GroupId);

        var expenseId = ExtractExpenseIds(change).OfType<Guid>().FirstOrDefault();
        if (expenseId != Guid.Empty && groupsByExpenseId.TryGetValue(expenseId, out var groupId))
        {
            change.SetExtraValue(AuditMetadataKeys.GroupId, groupId);
        }

        return Task.CompletedTask;
    }

    private static IEnumerable<object?> ExtractExpenseIds(AuditChange change)
    {
        if (change.Entity is ExpenseShare share && share.ExpenseId != Guid.Empty)
        {
            yield return share.ExpenseId;
            yield break;
        }

        foreach (var values in new[] { change.NewValues, change.OldValues })
        {
            if (values.TryGetValue(nameof(ExpenseShare.ExpenseId), out var value) && value is not null)
            {
                yield return value;
            }
        }
    }
}
