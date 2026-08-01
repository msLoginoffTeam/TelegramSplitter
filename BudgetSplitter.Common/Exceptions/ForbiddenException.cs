using Microsoft.AspNetCore.Http;

namespace BudgetSplitter.Common.Exceptions;

public class ForbiddenException(string message) : CustomException(message)
{
    public override int StatusCode => StatusCodes.Status403Forbidden;
}
