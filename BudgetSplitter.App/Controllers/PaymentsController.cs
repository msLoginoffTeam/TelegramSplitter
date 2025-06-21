using BudgetSplitter.App.Services.PaymentService;
using BudgetSplitter.Common.Dtos.Request;
using BudgetSplitter.Common.Dtos.Response;
using Microsoft.AspNetCore.Mvc;

namespace BudgetSplitter.App.Controllers
{
    [ApiController]
    [Route("api/groups/{groupId:guid}/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        public PaymentsController(IPaymentService paymentService) => _paymentService = paymentService;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PaymentResponseDto>>> GetPayments(Guid groupId)
        {
            var payments = await _paymentService.GetGroupPaymentsAsync(groupId);
            return Ok(payments);
        }

        [HttpPost("expense")]
        public async Task<ActionResult<PaymentResponseDto>> CreatePaymentForExpense(
            Guid groupId,
            [FromBody] CreatePaymentForExpenseRequestDto dto)
        {
            var result = await _paymentService.CreatePaymentForExpenseAsync(groupId, dto);
            return Ok(result);
        }

        [HttpPost("direct")]
        public async Task<ActionResult<PaymentResponseDto>> CreateDirectPayment(
            Guid groupId,
            [FromBody] CreateDirectPaymentRequestDto dto)
        {
            var result = await _paymentService.CreateDirectPaymentAsync(groupId, dto);
            return Ok(result);
        }

        [HttpPut("{paymentId:guid}")]
        public async Task<IActionResult> UpdatePayment(
            Guid groupId,
            Guid paymentId,
            [FromBody] UpdatePaymentRequestDto dto)
        {
            await _paymentService.UpdatePaymentAsync(groupId, paymentId, dto);
            return Ok();
        }

        [HttpDelete("{paymentId:guid}")]
        public async Task<IActionResult> DeletePayment(Guid groupId, Guid paymentId)
        {
            await _paymentService.DeletePaymentAsync(groupId, paymentId);
            return Ok();
        }
    }
}