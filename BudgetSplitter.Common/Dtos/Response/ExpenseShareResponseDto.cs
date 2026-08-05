namespace BudgetSplitter.Common.Dtos.Response;

public class ExpenseShareResponseDto
{
    public Guid UserId { get; set; }
    public string? DisplayName { get; set; }
    public string? Username { get; set; }
    public decimal Amount { get; set; }
    public bool IsPaid { get; set; }
}
