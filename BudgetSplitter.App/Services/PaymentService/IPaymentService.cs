using BudgetSplitter.Common.Dtos.Request;
using BudgetSplitter.Common.Dtos.Response;

namespace BudgetSplitter.App.Services.PaymentService;

public interface IPaymentService
{
    /// <summary>
    /// Получить все платежи в группе
    /// </summary>
    Task<IEnumerable<PaymentResponseDto>> GetGroupPaymentsAsync(Guid groupId);

    /// <summary>
    /// Получить платежи, где участвует пользователь (как отправитель или получатель)
    /// </summary>
    Task<IEnumerable<PaymentResponseDto>> GetUserPaymentsAsync(Guid groupId, Guid userId);

    /// <summary>
    /// Создать оплату конкретной траты
    /// </summary>
    Task<PaymentResponseDto> CreatePaymentForExpenseAsync(
        Guid groupId,
        CreatePaymentForExpenseRequestDto dto);

    /// <summary>
    /// Создать прямой перевод между участниками
    /// </summary>
    Task<PaymentResponseDto> CreateDirectPaymentAsync(
        Guid groupId,
        CreateDirectPaymentRequestDto dto);

    /// <summary>
    /// Обновить сумму платежа
    /// </summary>
    Task UpdatePaymentAsync(
        Guid groupId,
        Guid paymentId,
        UpdatePaymentRequestDto dto);

    /// <summary>
    /// Удалить платеж
    /// </summary>
    Task DeletePaymentAsync(Guid groupId, Guid paymentId);
}