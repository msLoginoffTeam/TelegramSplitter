namespace BudgetSplitter.Common.Dtos.Request;

/// <summary>
/// Request DTO для прямой оплаты между двумя пользователями
/// </summary>
public class CreateDirectPaymentRequestDto
{
    public Guid FromUserId { get; set; }
    public Guid ToUserId { get; set; }
    public decimal Amount { get; set; }
}