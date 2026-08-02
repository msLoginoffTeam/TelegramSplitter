namespace BudgetSplitter.Common.Domain;

public static class ExpenseShareSettlement
{
    public static bool IsPaid(Guid shareUserId, Guid payerId, decimal shareAmount, decimal paidAmount) =>
        shareUserId == payerId || paidAmount >= shareAmount;
}
