namespace BudgetSplitter.Common.Dtos;

/// <summary>
/// DTO для указания доли при создании/редактировании расхода
/// </summary>
public class ExpenseShareCreateDto
{
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
}