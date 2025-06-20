namespace BudgetSplitter.Common.Dtos.Response;

/// <summary>
/// Response DTO для истории операций внутри группы
/// </summary>
public class OperationHistoryResponseDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = null!;
    public Guid InitiatorUserId { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Description { get; set; }
}