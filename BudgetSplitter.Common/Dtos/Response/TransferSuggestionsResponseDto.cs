namespace BudgetSplitter.Common.Dtos.Response;

/// <summary>
/// Response DTO со списком оптимизированных переводов
/// </summary>
public class TransferSuggestionsResponseDto
{
    public List<TransferSuggestionDto> Transfers { get; set; } = new();
}