namespace BudgetSplitter.Common.Dtos.Request;

/// <summary>
/// Request DTO для оплаты конкретного расхода
/// </summary>
public class CreatePaymentForExpenseRequestDto
{
    public Guid ExpenseId { get; set; }
    public Guid FromUserId { get; set; }
    public decimal Amount { get; set; }
}