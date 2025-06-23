namespace BudgetSplitter.Common.Exceptions;

public interface ICustomProblemDetails
{
    string? Type { get; set; }
    string? Title { get; set; }
    int? Status { get; set; }
    string? Instance { get; set; }
}