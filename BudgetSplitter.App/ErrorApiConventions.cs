using BudgetSplitter.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

public static class ErrorApiConventions
{
    [ProducesResponseType(typeof(ICustomProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ICustomProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ICustomProblemDetails), StatusCodes.Status500InternalServerError)]
    public static void HandleErrors(
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        params object[] _ )
    {
    }
}