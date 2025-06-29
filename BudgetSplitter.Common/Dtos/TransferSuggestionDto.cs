namespace BudgetSplitter.Common.Dtos;

/// <summary>
/// Response DTO — предложение перевода между участниками
/// </summary>
public class TransferSuggestionDto
{
    public Guid FromUserId { get; set; }
    public string? FromUserName { get; set; }
    public Guid ToUserId { get; set; }
    public string? ToUserName { get; set; }
    public decimal Amount { get; set; }
}