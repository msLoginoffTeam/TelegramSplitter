namespace BudgetSplitter.Common.Exceptions;

public abstract class CustomException : Exception
{
    public abstract int StatusCode { get; }
    protected CustomException(string message) : base(message) { }
}