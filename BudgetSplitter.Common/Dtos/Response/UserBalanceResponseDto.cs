namespace BudgetSplitter.Common.Dtos.Response;

public class UserBalanceResponseDto
{
    public Guid UserId { get; set; }
    public decimal Balance { get; set; }
    
    public string? DisplayName { get; set; }
}