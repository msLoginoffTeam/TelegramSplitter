namespace BudgetSplitter.Common.Dtos.Request;

/// <summary>
/// Request DTO для создания или редактирования расхода
/// </summary>
public class CreateExpenseRequestDto
{
    public string Title { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public Guid CreatedById { get; set; }
    public bool IsDraft { get; set; }
    public List<ExpenseShareCreateDto> Shares { get; set; } = new();
}