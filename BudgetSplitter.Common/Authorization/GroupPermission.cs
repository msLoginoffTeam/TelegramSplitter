namespace BudgetSplitter.Common.Authorization;

public enum GroupPermission
{
    ViewGroup = 1,
    UpdateGroup = 2,
    ManageMembers = 3,
    ManagePermissions = 4,
    DeleteGroup = 5,
    TransferOwnership = 6,
    CreateExpense = 7,
    UpdateOwnExpense = 8,
    UpdateAnyExpense = 9,
    DeleteOwnExpense = 10,
    DeleteAnyExpense = 11,
    CreatePayment = 12,
    UpdateOwnPayment = 13,
    UpdateAnyPayment = 14,
    DeleteOwnPayment = 15,
    DeleteAnyPayment = 16
}
