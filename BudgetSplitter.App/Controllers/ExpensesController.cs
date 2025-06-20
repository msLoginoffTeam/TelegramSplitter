using BudgetSplitter.Common.Dtos.Request;
using BudgetSplitter.Common.Dtos.Response;
using Microsoft.AspNetCore.Mvc;

namespace BudgetSplitter.App.Controllers
{
    [ApiController]
    [Route("api/groups/{groupId:guid}/expenses")]
    public class ExpensesController : ControllerBase
    {
        public ExpensesController(/*IExpenseService svc*/) { /*…*/ }

        // GET api/groups/{groupId}/expenses
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExpenseResponseDto>>> GetExpenses(
            Guid groupId,
            [FromQuery] Guid? userId = null)
        {
            throw new NotImplementedException();
        }

        // GET api/groups/{groupId}/expenses/drafts
        [HttpGet("drafts")]
        public async Task<ActionResult<IEnumerable<ExpenseResponseDto>>> GetDraftExpenses(Guid groupId)
        {
            throw new NotImplementedException();
        }

        // GET api/groups/{groupId}/expenses/{expenseId}
        [HttpGet("{expenseId:guid}")]
        public async Task<ActionResult<ExpenseResponseDto>> GetExpense(Guid groupId, Guid expenseId)
        {
            throw new NotImplementedException();
        }

        // POST api/groups/{groupId}/expenses
        [HttpPost]
        public async Task<ActionResult<ExpenseResponseDto>> CreateExpense(
            Guid groupId,
            [FromBody] CreateExpenseRequestDto dto)
        {
            throw new NotImplementedException();
        }

        // PUT api/groups/{groupId}/expenses/{expenseId}
        [HttpPut("{expenseId:guid}")]
        public async Task<IActionResult> UpdateExpense(
            Guid groupId,
            Guid expenseId,
            [FromBody] UpdateExpenseRequestDto dto)
        {
            throw new NotImplementedException();
        }

        // DELETE api/groups/{groupId}/expenses/{expenseId}
        [HttpDelete("{expenseId:guid}")]
        public async Task<IActionResult> DeleteExpense(Guid groupId, Guid expenseId)
        {
            throw new NotImplementedException();
        }

        // POST api/groups/{groupId}/expenses/{expenseId}/confirm
        // [HttpPost("{expenseId:guid}/confirm")]
        // public async Task<IActionResult> ConfirmExpense(
        //     Guid groupId,
        //     Guid expenseId,
        //     [FromBody] ConfirmExpenseRequestDto dto)
        // {
        //     throw new NotImplementedException();
        // }
    }
}