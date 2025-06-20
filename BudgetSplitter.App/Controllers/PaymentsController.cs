using BudgetSplitter.Common.Dtos.Request;
using BudgetSplitter.Common.Dtos.Response;
using Microsoft.AspNetCore.Mvc;

namespace BudgetSplitter.App.Controllers
{
    [ApiController]
    [Route("api/groups/{groupId:guid}/payments")]
    public class PaymentsController : ControllerBase
    {
        public PaymentsController(/*IPaymentService svc*/) { /*…*/ }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PaymentResponseDto>>> GetPayments(Guid groupId)
        {
            throw new NotImplementedException();
        }

        [HttpPost("expense")]
        public async Task<ActionResult<PaymentResponseDto>> CreatePaymentForExpense(
            Guid groupId,
            [FromBody] CreatePaymentForExpenseRequestDto dto)
        {
            throw new NotImplementedException();
        }

        [HttpPost("direct")]
        public async Task<ActionResult<PaymentResponseDto>> CreateDirectPayment(
            Guid groupId,
            [FromBody] CreateDirectPaymentRequestDto dto)
        {
            throw new NotImplementedException();
        }

        [HttpPut("{paymentId:guid}")]
        public async Task<IActionResult> UpdatePayment(
            Guid groupId,
            Guid paymentId,
            [FromBody] UpdatePaymentRequestDto dto)
        {
            throw new NotImplementedException();
        }

        [HttpDelete("{paymentId:guid}")]
        public async Task<IActionResult> DeletePayment(Guid groupId, Guid paymentId)
        {
            throw new NotImplementedException();
        }
    }
}