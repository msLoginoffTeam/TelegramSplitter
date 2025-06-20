namespace BudgetSplitter.Common.Dtos.Request;

/// <summary>
/// Request DTO для частичного обновления расхода
/// </summary>
public class UpdateExpenseRequestDto
{
    public string? Title { get; set; }
    public decimal? TotalAmount { get; set; }
    public bool? IsDraft { get; set; }
    public List<ExpenseShareCreateDto>? Shares { get; set; }
}