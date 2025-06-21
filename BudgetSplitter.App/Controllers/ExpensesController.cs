using BudgetSplitter.App.Services.ExpenseService;
using BudgetSplitter.Common.Dtos;
using BudgetSplitter.Common.Dtos.Request;
using BudgetSplitter.Common.Dtos.Response;
using Microsoft.AspNetCore.Mvc;

namespace BudgetSplitter.App.Controllers
{
    [ApiController]
    [Route("api/groups/{groupId:guid}/expenses")]
    public class ExpensesController : ControllerBase
    {
        private readonly IExpenseService _expenseService;
        public ExpensesController(IExpenseService expenseService) => _expenseService = expenseService;

        // GET api/groups/{groupId}/expenses
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExpenseResponseDto>>> GetExpenses(
            Guid groupId,
            [FromQuery] Guid? userId = null)
        {
            var expenses = await _expenseService.GetGroupExpensesAsync(groupId);
            return Ok(expenses);
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
            var expense = await _expenseService.GetExpenseByIdAsync(groupId, expenseId);
            return Ok(expense);
        }

        // POST api/groups/{groupId}/expenses
        [HttpPost]
        public async Task<ActionResult<ExpenseResponseDto>> CreateExpense(
            Guid groupId,
            [FromBody] CreateExpenseRequestDto dto)
        {
            var response = await _expenseService.CreateExpenseAsync(groupId, dto);
            return Ok(response);
        }

        // PUT api/groups/{groupId}/expenses/{expenseId}
        [HttpPut("{expenseId:guid}")]
        public async Task<IActionResult> UpdateExpense(
            Guid groupId,
            Guid expenseId,
            [FromBody] UpdateExpenseRequestDto dto)
        {
            await _expenseService.UpdateExpenseAsync(groupId, expenseId, dto);
            return Ok();
        }

        // DELETE api/groups/{groupId}/expenses/{expenseId}
        [HttpDelete("{expenseId:guid}")]
        public async Task<IActionResult> DeleteExpense(Guid groupId, Guid expenseId)
        {
            await _expenseService.DeleteExpenseAsync(groupId, expenseId);
            return Ok();
        }
        
        /// <summary>
        /// Получить участников траты
        /// </summary>
        [HttpGet("{expenseId:guid}/participants")]
        public async Task<ActionResult<IEnumerable<ExpenseShareResponseDto>>> GetExpenseParticipants(
            Guid groupId,
            Guid expenseId)
        {
            var shares = await _expenseService.GetExpenseParticipantsAsync(groupId, expenseId);
            return Ok(shares);
        }

        /// <summary>
        /// Добавить участников к трате
        /// </summary>
        [HttpPost("{expenseId:guid}/participants")]
        public async Task<IActionResult> AddExpenseParticipants(
            Guid groupId,
            Guid expenseId,
            [FromBody] IEnumerable<ExpenseShareCreateDto> shares)
        {
            await _expenseService.AddExpenseParticipantsAsync(groupId, expenseId, shares);
            return Ok();
        }

        /// <summary>
        /// Обновить долю участника в трате
        /// </summary>
        [HttpPut("{expenseId:guid}/participants/{userId:guid}")]
        public async Task<IActionResult> UpdateExpenseParticipant(
            Guid groupId,
            Guid expenseId,
            [FromBody] ExpenseShareCreateDto share)
        {
            await _expenseService.UpdateExpenseParticipantAsync(groupId, expenseId, share);
            return Ok();
        }

        /// <summary>
        /// Удалить участника из траты
        /// </summary>
        [HttpDelete("{expenseId:guid}/participants/{userId:guid}")]
        public async Task<IActionResult> RemoveExpenseParticipant(
            Guid groupId,
            Guid expenseId,
            Guid userId)
        {
            await _expenseService.RemoveExpenseParticipantAsync(groupId, expenseId, userId);
            return Ok();
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