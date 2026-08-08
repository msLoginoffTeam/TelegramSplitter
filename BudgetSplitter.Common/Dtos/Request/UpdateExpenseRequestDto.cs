namespace BudgetSplitter.Common.Dtos.Request;

/// <summary>
/// Request DTO для полного обновления расхода
/// </summary>
public class UpdateExpenseRequestDto
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public decimal TotalAmount { get; set; }
    public Guid PayerId { get; set; }
    public List<ExpenseShareCreateDto> Shares { get; set; } = new();
}
