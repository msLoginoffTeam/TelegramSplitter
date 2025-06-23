using Microsoft.AspNetCore.Http;

namespace BudgetSplitter.Common.Exceptions;

public class BadRequestException : CustomException
{
    public override int StatusCode => StatusCodes.Status400BadRequest;
    public BadRequestException(string message) : base(message) { }
}