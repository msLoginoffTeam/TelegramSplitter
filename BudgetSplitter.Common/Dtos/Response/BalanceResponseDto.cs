namespace BudgetSplitter.Common.Dtos.Response;

/// <summary>
/// Response DTO со списком балансов всех участников
/// </summary>
public class BalanceResponseDto
{
    public List<UserBalanceResponseDto> Balances { get; set; } = new();
}