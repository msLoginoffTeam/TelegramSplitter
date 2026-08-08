namespace BudgetSplitter.Common.Dtos.Response;

public class PaymentResponseDto
{
    public Guid Id { get; set; }
    public Guid? ExpenseId { get; set; }
    public Guid FromUserId { get; set; }
    public string? FromDisplayName { get; set; }
    public string? FromUsername { get; set; }
    public Guid ToUserId { get; set; }
    public string? ToDisplayName { get; set; }
    public string? ToUsername { get; set; }
    public Guid CreatedByUserId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime Timestamp { get; set; }
}
