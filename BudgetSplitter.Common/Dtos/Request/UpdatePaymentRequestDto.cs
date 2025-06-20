namespace BudgetSplitter.Common.Dtos.Request;

/// <summary>
/// Request DTO для редактирования суммы платежа
/// </summary>
public class UpdatePaymentRequestDto
{
    public decimal Amount { get; set; }
}