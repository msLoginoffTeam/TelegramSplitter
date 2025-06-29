namespace BudgetSplitter.Common.Dtos.Response;

public class ExpenseShareResponseDto
{
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public decimal Amount { get; set; }
    public bool IsPaid { get; set; }
}