using BudgetSplitter.Common.Dtos.Response;
using Microsoft.AspNetCore.Mvc;

namespace BudgetSplitter.App.Controllers
{
    [ApiController]
    [Route("api/groups/{groupId:guid}")]
    public class BalanceController : ControllerBase
    {
        public BalanceController(/*IBalanceService svc*/) { /*…*/ }

        /// <summary>
        /// Returns the current balance summary for the specified group.
        /// </summary>
        /// <param name="groupId">ID of the group to get balances for.</param>
        /// <returns>BalanceResponseDto containing each user’s net balance.</returns>
        [HttpGet("balance")]
        public async Task<ActionResult<BalanceResponseDto>> GetBalance(Guid groupId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Calculates and returns suggested transfers to settle debts within the group.
        /// </summary>
        /// <param name="groupId">ID of the group to suggest transfers for.</param>
        /// <param name="useNpAlgorithm">
        /// If true, use the NP-complete optimization algorithm for minimal transactions;
        /// otherwise use the greedy algorithm.
        /// </param>
        /// <returns>TransferSuggestionsResponseDto with transfer recommendations.</returns>
        [HttpGet("transfers")]
        public async Task<ActionResult<TransferSuggestionsResponseDto>> GetSuggestedTransfers(
            Guid groupId,
            [FromQuery] bool useNpAlgorithm = false)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Retrieves the history of operations (expenses and payments) for the group.
        /// </summary>
        /// <param name="groupId">ID of the group to retrieve history for.</param>
        /// <returns>List of OperationHistoryResponseDto entries in chronological order.</returns>
        [HttpGet("history")]
        public async Task<ActionResult<IEnumerable<OperationHistoryResponseDto>>> GetHistory(
            Guid groupId)
        {
            throw new NotImplementedException();
        }
    }
}