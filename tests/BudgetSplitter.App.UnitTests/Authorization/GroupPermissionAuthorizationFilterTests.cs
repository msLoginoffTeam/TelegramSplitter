using System.Reflection;
using BudgetSplitter.App.Authorization;
using BudgetSplitter.Common.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Persistence;

namespace BudgetSplitter.App.UnitTests.Authorization;

public sealed class GroupPermissionAuthorizationFilterTests
{
    [Fact]
    public async Task OnAuthorizationAsync_RequiresEveryPermissionDeclaredOnAttribute()
    {
        var context = CreateContext();
        var filter = new GroupPermissionAuthorizationFilter(new StubGroupAuthorizationService(
            GroupPermission.ViewGroup,
            GroupPermission.CreateExpense));

        await filter.OnAuthorizationAsync(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public async Task OnAuthorizationAsync_WhenOneDeclaredPermissionIsMissing_ReturnsForbidden()
    {
        var context = CreateContext();
        var filter = new GroupPermissionAuthorizationFilter(new StubGroupAuthorizationService(GroupPermission.ViewGroup));

        await filter.OnAuthorizationAsync(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }

    [Fact]
    public void RequireGroupPermissionAttribute_WithoutPermissions_Throws()
    {
        Assert.Throws<ArgumentException>(() => new RequireGroupPermissionAttribute());
    }

    private static AuthorizationFilterContext CreateContext()
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(new RouteValueDictionary { ["groupId"] = Guid.NewGuid().ToString() }),
            new ControllerActionDescriptor
            {
                ControllerTypeInfo = typeof(TestController).GetTypeInfo(),
                MethodInfo = typeof(TestController).GetMethod(nameof(TestController.Action))!
            });
        return new AuthorizationFilterContext(actionContext, []);
    }

    private sealed class TestController
    {
        [RequireGroupPermission(GroupPermission.ViewGroup, GroupPermission.CreateExpense)]
        public void Action()
        {
        }
    }

    private sealed class StubGroupAuthorizationService(params GroupPermission[] permissions) : IGroupAuthorizationService
    {
        public Task EnsurePermissionAsync(Guid groupId, GroupPermission permission, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task EnsureOwnerAsync(Guid groupId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task EnsureExpensePermissionAsync(Guid groupId, Guid expenseId, GroupPermission ownPermission, GroupPermission anyPermission, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task EnsurePaymentPermissionAsync(Guid groupId, Guid paymentId, GroupPermission ownPermission, GroupPermission anyPermission, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<UserGroup> GetMembershipAsync(Guid groupId, CancellationToken cancellationToken = default) => Task.FromResult(new UserGroup
        {
            GroupId = groupId,
            UserId = Guid.NewGuid(),
            Permissions = permissions.Select(permission => new GroupMemberPermission { Permission = permission }).ToList()
        });
    }
}
