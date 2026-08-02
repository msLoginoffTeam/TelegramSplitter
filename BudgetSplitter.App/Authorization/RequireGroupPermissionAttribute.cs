using BudgetSplitter.Common.Authorization;
using BudgetSplitter.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Reflection;

namespace BudgetSplitter.App.Authorization;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class RequireGroupPermissionAttribute(params GroupPermission[] permissions) : Attribute
{
    public IReadOnlyList<GroupPermission> Permissions { get; } = permissions.Length > 0
        ? permissions
        : throw new ArgumentException("At least one group permission is required.", nameof(permissions));
}

public sealed class GroupPermissionAuthorizationFilter(IGroupAuthorizationService groupAuthorization) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.ActionDescriptor is not ControllerActionDescriptor actionDescriptor) return;
        var requirements = actionDescriptor.MethodInfo
            .GetCustomAttributes<RequireGroupPermissionAttribute>(inherit: true)
            .ToArray();
        if (requirements.Length == 0) return;

        if (!Guid.TryParse(context.RouteData.Values["groupId"]?.ToString(), out var groupId))
        {
            context.Result = new BadRequestObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "A valid groupId route parameter is required."
            });
            return;
        }

        try
        {
            var membership = await groupAuthorization.GetMembershipAsync(groupId, context.HttpContext.RequestAborted);
            var grantedPermissions = membership.Permissions.Select(grant => grant.Permission).ToHashSet();

            var requiredPermissions = requirements
                .SelectMany(requirement => requirement.Permissions)
                .ToHashSet();

            if (!requiredPermissions.IsSubsetOf(grantedPermissions))
            {
                throw new ForbiddenException("All required group permissions must be granted.");
            }
        }
        catch (ForbiddenException exception)
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = exception.Message
            }) { StatusCode = StatusCodes.Status403Forbidden };
        }
    }

}
