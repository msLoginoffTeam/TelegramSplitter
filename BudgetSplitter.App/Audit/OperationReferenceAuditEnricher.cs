using AuditLogLens.Changes;
using AuditLogLens.Enrichment;
using AuditLogLens.Enrichment.Context;
using AuditLogLens.Enrichment.Extensions;
using Persistence;

namespace BudgetSplitter.App.Audit;

public sealed class OperationReferenceAuditEnricher : AuditEntityEnricherBase
{
    public override bool CanHandle(Type entityType)
        => entityType is not null &&
           (entityType == typeof(Expense) || entityType == typeof(ExpenseShare) || entityType == typeof(Payment));

    public override void Configure(IAuditEnrichmentPlanBuilder builder)
    {
        builder.Lookup<Expense, Guid>(expense => expense.Id, ExtractExpenseIds);
        builder.Lookup<User, Guid>(user => user.Id, ExtractUserIds);
    }

    protected override Task AfterMergeChangeAsync(
        AuditEnrichmentContext context,
        AuditChange change,
        CancellationToken cancellationToken = default)
    {
        var expensesById = context
            .GetLoaded<Expense>(nameof(Expense.Id))
            .ToDictionary(expense => expense.Id);
        var usersById = context
            .GetLoaded<User>(nameof(User.Id))
            .ToDictionary(user => user.Id, FormatUser);

        var expenseId = ExtractExpenseIds(change).OfType<Guid>().FirstOrDefault();
        if (expenseId != Guid.Empty && expensesById.TryGetValue(expenseId, out var expense))
        {
            if (!change.ExtraValues.ContainsKey(AuditMetadataKeys.GroupId))
            {
                change.SetExtraValue(AuditMetadataKeys.GroupId, expense.GroupId);
            }
            AddContextValue(change, "ExpenseTitle", expense.Title);
        }

        if (change.Entity is Expense operationExpense)
        {
            AddContextValue(change, "ExpenseTitle", operationExpense.Title, onlyWhenMissing: true);
        }

        foreach (var reference in ExtractUserReferences(change))
        {
            if (usersById.TryGetValue(reference.UserId, out var userName))
            {
                AddContextValue(change, reference.FieldName, userName);
            }
        }

        return Task.CompletedTask;
    }

    private static IEnumerable<object?> ExtractExpenseIds(AuditChange change)
    {
        var expenseId = change.Entity switch
        {
            ExpenseShare share => share.ExpenseId,
            Payment payment => payment.ExpenseId,
            _ => null
        };
        if (expenseId is { } currentExpenseId && currentExpenseId != Guid.Empty)
        {
            yield return currentExpenseId;
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

    private static IEnumerable<object?> ExtractUserIds(AuditChange change)
    {
        return ExtractUserReferences(change).Select(reference => (object?)reference.UserId);
    }

    private static IEnumerable<UserReference> ExtractUserReferences(AuditChange change)
    {
        switch (change.Entity)
        {
            case ExpenseShare share when share.UserId != Guid.Empty:
                yield return new UserReference("Participant", share.UserId);
                yield break;
            case Payment payment:
                if (payment.FromUserId != Guid.Empty)
                {
                    yield return new UserReference("FromParticipant", payment.FromUserId);
                }
                if (payment.ToUserId != Guid.Empty)
                {
                    yield return new UserReference("ToParticipant", payment.ToUserId);
                }
                if (payment.CreatedByUserId != Guid.Empty)
                {
                    yield return new UserReference("Author", payment.CreatedByUserId);
                }
                yield break;
            case Expense expense:
                if (expense.PayerId != Guid.Empty)
                {
                    yield return new UserReference("Payer", expense.PayerId);
                }
                if (expense.CreatedByUserId != Guid.Empty)
                {
                    yield return new UserReference("Author", expense.CreatedByUserId);
                }
                yield break;
        }
    }

    private static void AddContextValue(
        AuditChange change,
        string fieldName,
        object value,
        bool onlyWhenMissing = false)
    {
        if (change.OldValues.Count > 0 && (!onlyWhenMissing || !change.OldValues.ContainsKey(fieldName)))
        {
            change.OldValues.TryAdd(fieldName, value);
        }

        if (change.NewValues.Count > 0 && (!onlyWhenMissing || !change.NewValues.ContainsKey(fieldName)))
        {
            change.NewValues.TryAdd(fieldName, value);
        }
    }

    private static string FormatUser(User user)
        => !string.IsNullOrWhiteSpace(user.DisplayName)
            ? user.DisplayName
            : !string.IsNullOrWhiteSpace(user.Username)
                ? $"@{user.Username}"
                : user.TelegramId.ToString();

    private sealed record UserReference(string FieldName, Guid UserId);
}
