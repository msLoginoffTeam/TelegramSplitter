namespace BudgetSplitter.Common.Dtos.Request;

public class UserCreateRequestDto
{
    public long TelegramId { get; set; }
    public string? DisplayName { get; set; }
}