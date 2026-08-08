using AuditLogLens.Restrictions;
using Persistence;

namespace BudgetSplitter.App.Audit;

public sealed class BudgetSplitterAuditRestrictions : AuditRestrictionsBase
{
    protected override void Configure(AuditRestrictionRules rules)
    {
        rules.For<Group>();
        rules.For<Expense>();
        rules.For<ExpenseShare>();
        rules.For<Payment>();
    }
}
