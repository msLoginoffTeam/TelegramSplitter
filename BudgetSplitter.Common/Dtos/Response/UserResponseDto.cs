namespace BudgetSplitter.Common.Dtos.Response;

public class UserResponseDto
{
    public Guid Id { get; set; }
    public long TelegramId { get; set; }
    public string? DisplayName { get; set; }
    public IEnumerable<GroupResponseDto>? Groups { get; set; }
}