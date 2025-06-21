namespace BudgetSplitter.Common.Dtos.Response;

public class UserOverviewResponseDto
{
    public Guid Id { get; set; }
    public long TelegramId { get; set; }
    public string? DisplayName { get; set; }
}