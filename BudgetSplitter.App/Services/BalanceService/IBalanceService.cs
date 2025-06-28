using BudgetSplitter.Common.Dtos.Response;

namespace BudgetSplitter.App.Services.BalanceService;

public interface IBalanceService
{
    Task<BalanceResponseDto> GetBalancesAsync(Guid groupId);
    Task<TransferSuggestionsResponseDto> GetSuggestedTransfersAsync(Guid groupId, bool useNp);
    Task<IEnumerable<OperationHistoryResponseDto>> GetHistoryAsync(Guid groupId);
}