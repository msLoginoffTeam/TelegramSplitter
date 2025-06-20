using BudgetSplitter.Common.Dtos.Response;
using Microsoft.AspNetCore.Mvc;

namespace BudgetSplitter.App.Controllers
{
    [ApiController]
    [Route("api/groups/{groupId:guid}")]
    public class BalanceController : ControllerBase
    {
        public BalanceController(/*IBalanceService svc*/) { /*…*/ }

        [HttpGet("balance")]
        public async Task<ActionResult<BalanceResponseDto>> GetBalance(Guid groupId)
        {
            throw new NotImplementedException();
        }

        [HttpGet("transfers")]
        public async Task<ActionResult<TransferSuggestionsResponseDto>> GetSuggestedTransfers(
            Guid groupId,
            [FromQuery] bool useNpAlgorithm = false)
        {
            throw new NotImplementedException();
        }

        [HttpGet("history")]
        public async Task<ActionResult<IEnumerable<OperationHistoryResponseDto>>> GetHistory(
            Guid groupId)
        {
            throw new NotImplementedException();
        }
    }
}