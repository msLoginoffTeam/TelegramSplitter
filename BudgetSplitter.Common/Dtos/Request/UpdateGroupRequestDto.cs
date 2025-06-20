namespace BudgetSplitter.Common.Dtos.Request;

/// <summary>
/// Request DTO для обновления группы (переименования)
/// </summary>
public class UpdateGroupRequestDto
{
    public string Title { get; set; } = null!;
}