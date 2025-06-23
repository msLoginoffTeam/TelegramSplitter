namespace BudgetSplitter.Common.Dtos.Request;

public class UserCreateRequestDto
{
    public Guid Id { get; set; }
    public long TelegramId { get; set; }
    public string? DisplayName { get; set; }
}