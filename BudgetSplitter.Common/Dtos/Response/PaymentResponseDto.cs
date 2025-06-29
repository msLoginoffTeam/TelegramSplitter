namespace BudgetSplitter.Common.Dtos.Response;

public class PaymentResponseDto
{
    public Guid Id { get; set; }
    public Guid? ExpenseId { get; set; }
    public Guid FromUserId { get; set; }
    public string? FromUserName { get; set; }
    public Guid ToUserId { get; set; }
    public string? ToUserName { get; set; }
    public decimal Amount { get; set; }
    public DateTime Timestamp { get; set; }
}