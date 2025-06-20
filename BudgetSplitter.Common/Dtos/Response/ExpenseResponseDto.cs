namespace BudgetSplitter.Common.Dtos.Response;

/// <summary>
/// Response DTO с полной информацией по расходу
/// </summary>
public class ExpenseResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public Guid CreatedById { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsDraft { get; set; }
    public List<ExpenseShareResponseDto> Shares { get; set; } = new();
}