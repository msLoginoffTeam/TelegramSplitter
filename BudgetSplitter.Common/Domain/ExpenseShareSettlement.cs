namespace BudgetSplitter.Common.Domain;

public static class ExpenseShareSettlement
{
    public static bool IsPaidByPayments(Guid shareUserId, Guid payerId, decimal shareAmount, decimal paidAmount) =>
        shareUserId == payerId || paidAmount >= shareAmount;

    public static bool IsSettled(
        Guid shareUserId,
        Guid payerId,
        decimal shareAmount,
        decimal paidAmount,
        bool isManuallySettled) =>
        IsPaidByPayments(shareUserId, payerId, shareAmount, paidAmount) || isManuallySettled;
}
