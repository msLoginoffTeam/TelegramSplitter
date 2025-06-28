using BudgetSplitter.App.Services.ExpenseService;
using BudgetSplitter.Common.Dtos;
using BudgetSplitter.Common.Dtos.Request;
using BudgetSplitter.Common.Dtos.Response;
using Microsoft.AspNetCore.Mvc;

namespace BudgetSplitter.App.Controllers
{
    /// <summary>
    /// Controller for managing expenses within a group.
    /// </summary>
    [ApiController]
    [Route("api/expenses")]
    public class ExpensesController : ControllerBase
    {
        private readonly IExpenseService _expenseService;
        public ExpensesController(IExpenseService expenseService) => _expenseService = expenseService;

        /// <summary>
        /// Retrieves all confirmed expenses in the group, optionally filtered by a specific user.
        /// </summary>
        /// <param name="groupId">ID of the group.</param>
        /// <param name="userId">Optional user ID to filter expenses by payer.</param>
        /// <returns>List of ExpenseResponseDto.</returns>
        [HttpGet("group/{groupId:guid}")]
        public async Task<ActionResult<IEnumerable<ExpenseResponseDto>>> GetExpenses(
            Guid groupId,
            [FromQuery] Guid? userId = null)
        {
            var expenses = await _expenseService.GetGroupExpensesAsync(groupId);
            return Ok(expenses);
        }

        // /// <summary>
        // /// Retrieves all draft (unconfirmed) expenses in the group.
        // /// </summary>
        // /// <param name="groupId">ID of the group.</param>
        // /// <returns>List of draft ExpenseResponseDto.</returns>
        // [HttpGet("drafts")]
        // public async Task<ActionResult<IEnumerable<ExpenseResponseDto>>> GetDraftExpenses(Guid groupId)
        // {
        //     throw new NotImplementedException();
        // }

        /// <summary>
        /// Retrieves details for a specific expense.
        /// </summary>
        /// <param name="groupId">ID of the group.</param>
        /// <param name="expenseId">ID of the expense.</param>
        /// <returns>ExpenseResponseDto with full expense details.</returns>
        [HttpGet("{expenseId:guid}")]
        public async Task<ActionResult<ExpenseResponseDto>> GetExpense(Guid groupId, Guid expenseId)
        {
            var expense = await _expenseService.GetExpenseByIdAsync(groupId, expenseId);
            return Ok(expense);
        }

        /// <summary>
        /// Creates a new expense in the group.
        /// </summary>
        /// <param name="groupId">ID of the group.</param>
        /// <param name="dto">Data for creating the expense.</param>
        /// <returns>The created ExpenseResponseDto.</returns>
        [HttpPost("group/{groupId:guid}")]
        public async Task<ActionResult<ExpenseResponseDto>> CreateExpense(
            Guid groupId,
            [FromBody] CreateExpenseRequestDto dto)
        {
            var response = await _expenseService.CreateExpenseAsync(groupId, dto);
            return Ok(response);
        }

        /// <summary>
        /// Updates an existing expense’s title
        /// </summary>
        /// <param name="expenseId">ID of the expense to update.</param>
        /// <param name="dto">Fields to update.</param>
        /// <param name="title"></param>
        [HttpPut("{expenseId:guid}/title")]
        public async Task<IActionResult> UpdateExpenseTitle(
            //Guid groupId,
            Guid expenseId,
            [FromBody] string title)
        {
            await _expenseService.UpdateExpenseAsync(expenseId, title);
            return Ok();
        }

        /// <summary>
        /// Updates an existing expense’s total amount.
        /// </summary>
        /// <param name="expenseId">ID of the expense to update.</param>
        /// <param name="totalAmount"></param>
        [HttpPut("{expenseId:guid}/totalAmount")]
        public async Task<IActionResult> UpdateExpenseTotalAmount(
            //Guid groupId,
            Guid expenseId,
            [FromBody] decimal totalAmount)
        {
            await _expenseService.UpdateExpenseAsync(expenseId, totalAmount);
            return Ok();
        }

        /// <summary>
        /// Deletes an expense from the group.
        /// </summary>
        /// <param name="groupId">ID of the group.</param>
        /// <param name="expenseId">ID of the expense to delete.</param>
        [HttpDelete("{expenseId:guid}")]
        public async Task<IActionResult> DeleteExpense(Guid groupId, Guid expenseId)
        {
            await _expenseService.DeleteExpenseAsync(groupId, expenseId);
            return Ok();
        }
        
        /// <summary>
        /// Retrieves all participants and their shares for an expense.
        /// </summary>
        /// <param name="groupId">ID of the group.</param>
        /// <param name="expenseId">ID of the expense.</param>
        /// <returns>List of ExpenseShareResponseDto.</returns>
        [HttpGet("{expenseId:guid}/participants")]
        public async Task<ActionResult<IEnumerable<ExpenseShareResponseDto>>> GetExpenseParticipants(
            Guid groupId,
            Guid expenseId)
        {
            var shares = await _expenseService.GetExpenseParticipantsAsync(groupId, expenseId);
            return Ok(shares);
        }

        /// <summary>
        /// Adds a participant share to the expense and adjusts the payer’s share accordingly.
        /// </summary>
        /// <param name="groupId">ID of the group.</param>
        /// <param name="expenseId">ID of the expense.</param>
        /// <param name="share">Share details of the new participant.</param>
        [HttpPost("{expenseId:guid}/participants")]
        public async Task<IActionResult> AddExpenseParticipants(
            Guid groupId,
            Guid expenseId,
            [FromBody] ExpenseShareCreateDto share)
        {
            await _expenseService.AddExpenseParticipantsAsync(groupId, expenseId, share);
            return Ok();
        }

        /// <summary>
        /// Updates the share amount for an existing participant in an expense.
        /// </summary>
        /// <param name="groupId">ID of the group.</param>
        /// <param name="expenseId">ID of the expense.</param>
        /// <param name="share">Updated share details for the participant.</param>
        [HttpPut("{expenseId:guid}/participants")]
        public async Task<IActionResult> UpdateExpenseParticipant(
            Guid groupId,
            Guid expenseId,
            [FromBody] ExpenseShareCreateDto share)
        {
            await _expenseService.UpdateExpenseParticipantAsync(groupId, expenseId, share);
            return Ok();
        }

        /// <summary>
        /// Removes a participant from an expense and reallocates their share to the payer.
        /// </summary>
        /// <param name="groupId">ID of the group.</param>
        /// <param name="expenseId">ID of the expense.</param>
        /// <param name="userId">ID of the participant to remove.</param>
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