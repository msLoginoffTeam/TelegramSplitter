using BudgetSplitter.App.Services.PaymentService;
using BudgetSplitter.Common.Dtos.Request;
using BudgetSplitter.Common.Dtos.Response;
using Microsoft.AspNetCore.Mvc;

namespace BudgetSplitter.App.Controllers
{
    /// <summary>
    /// Controller for managing payments within a group, both for expenses and direct transfers.
    /// </summary>
    [ApiController]
    [Route("api/groups/{groupId:guid}/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        public PaymentsController(IPaymentService paymentService) => _paymentService = paymentService;

        /// <summary>
        /// Retrieves all payments (expense-based and direct) in the group.
        /// </summary>
        /// <param name="groupId">ID of the group.</param>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PaymentResponseDto>>> GetPayments(Guid groupId)
        {
            var payments = await _paymentService.GetGroupPaymentsAsync(groupId);
            return Ok(payments);
        }
        
        /// <summary>
        /// Records a payment against a specific expense.
        /// </summary>
        /// <param name="groupId">ID of the group.</param>
        /// <param name="dto">Payment details (expense ID, payer, amount).</param>
        /// <returns>The created PaymentResponseDto.</returns>
        [HttpPost("expense")]
        public async Task<ActionResult<PaymentResponseDto>> CreatePaymentForExpense(
            Guid groupId,
            [FromBody] CreatePaymentForExpenseRequestDto dto)
        {
            var result = await _paymentService.CreatePaymentForExpenseAsync(groupId, dto);
            return Ok(result);
        }

        /// <summary>
        /// Records a direct payment (transfer) between two users in the group.
        /// </summary>
        /// <param name="groupId">ID of the group.</param>
        /// <param name="dto">Direct payment details (from, to, amount).</param>
        /// <returns>The created PaymentResponseDto.</returns>
        [HttpPost("direct")]
        public async Task<ActionResult<PaymentResponseDto>> CreateDirectPayment(
            Guid groupId,
            [FromBody] CreateDirectPaymentRequestDto dto)
        {
            var result = await _paymentService.CreateDirectPaymentAsync(groupId, dto);
            return Ok(result);
        }

        /// <summary>
        /// Updates an existing payment’s amount or recipient.
        /// </summary>
        /// <param name="groupId">ID of the group.</param>
        /// <param name="paymentId">ID of the payment.</param>
        /// <param name="dto">Updated payment data.</param>
        [HttpPut("{paymentId:guid}")]
        public async Task<IActionResult> UpdatePayment(
            Guid groupId,
            Guid paymentId,
            [FromBody] UpdatePaymentRequestDto dto)
        {
            await _paymentService.UpdatePaymentAsync(groupId, paymentId, dto);
            return Ok();
        }

        /// <summary>
        /// Deletes a payment record.
        /// </summary>
        /// <param name="groupId">ID of the group.</param>
        /// <param name="paymentId">ID of the payment to delete.</param>
        [HttpDelete("{paymentId:guid}")]
        public async Task<IActionResult> DeletePayment(Guid groupId, Guid paymentId)
        {
            await _paymentService.DeletePaymentAsync(groupId, paymentId);
            return Ok();
        }
    }
}