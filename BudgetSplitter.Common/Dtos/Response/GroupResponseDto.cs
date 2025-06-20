namespace BudgetSplitter.Common.Dtos.Response;

/// <summary>
/// Response DTO с полной информацией по группе
/// </summary>
public class GroupResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public long? TelegramChatId { get; set; }
    public List<UserResponseDto> Users { get; set; } = new();
}