namespace BudgetSplitter.Common.Dtos.Request;

/// <summary>
/// Request DTO для создания новой группы
/// </summary>
public class CreateGroupRequestDto
{
    public string Title { get; set; } = null!;
    
    public long CreatedByTelegramId { get; set; }
    public long? TelegramChatId { get; set; }
}