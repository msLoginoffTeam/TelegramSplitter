using System.Net;
using System.Net.Http.Json;
using BudgetSplitter.App.Authentication;
using BudgetSplitter.App.IntegrationTests.Infrastructure;
using BudgetSplitter.Common.Authorization;
using BudgetSplitter.Common.Dtos.Request;
using BudgetSplitter.Tests.Shared;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace BudgetSplitter.App.IntegrationTests.Authorization;

public sealed class GroupAuthorizationTests(PostgreSqlFixture database) : IntegrationTestBase(database)
{
    [Fact]
    public async Task GroupDetails_NonMemberIsForbidden()
    {
        var data = await SeedGroupAsync();

        using var response = await SendAuthenticatedAsync(HttpMethod.Get, $"/api/groups/{data.GroupId}", TelegramIds.NonMember);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GroupDetails_ViewerCanRead()
    {
        var data = await SeedGroupAsync();

        using var response = await SendAuthenticatedAsync(HttpMethod.Get, $"/api/groups/{data.GroupId}", TelegramIds.Viewer);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateExpense_ViewerIsForbidden()
    {
        var data = await SeedGroupAsync();
        var ownerId = data.UserIds[TelegramIds.Owner];
        var requestDto = new CreateExpenseRequestDto
        {
            Title = "Dinner",
            TotalAmount = 100,
            PayerId = ownerId
        };

        using var response = await SendAuthenticatedAsync(
            HttpMethod.Post,
            $"/api/groups/{data.GroupId}/expenses",
            TelegramIds.Viewer,
            JsonContent.Create(requestDto));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateExpense_MemberCanUpdateOwnButNotAnotherUsersExpense()
    {
        var data = await SeedGroupAsync();
        var ownExpenseId = await SeedExpenseAsync(data.GroupId, data.UserIds[TelegramIds.Owner], data.UserIds[TelegramIds.Member]);
        var anotherExpenseId = await SeedExpenseAsync(data.GroupId, data.UserIds[TelegramIds.Owner], data.UserIds[TelegramIds.Owner]);

        using var ownResponse = await SendAuthenticatedAsync(
            HttpMethod.Put,
            $"/api/groups/{data.GroupId}/expenses/{ownExpenseId}/title",
            TelegramIds.Member,
            JsonContent.Create("Updated by author"));
        using var anotherResponse = await SendAuthenticatedAsync(
            HttpMethod.Put,
            $"/api/groups/{data.GroupId}/expenses/{anotherExpenseId}/title",
            TelegramIds.Member,
            JsonContent.Create("Attempted update"));

        Assert.Equal(HttpStatusCode.OK, ownResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, anotherResponse.StatusCode);
        await using var db = CreateDbContext();
        Assert.Equal("Updated by author", await db.Expenses.Where(expense => expense.Id == ownExpenseId).Select(expense => expense.Title).SingleAsync());
    }

    [Fact]
    public async Task UpdateExpense_AdminCanUpdateAnotherUsersExpense()
    {
        var data = await SeedGroupAsync();
        var expenseId = await SeedExpenseAsync(data.GroupId, data.UserIds[TelegramIds.Owner], data.UserIds[TelegramIds.Owner]);

        using var response = await SendAuthenticatedAsync(
            HttpMethod.Put,
            $"/api/groups/{data.GroupId}/expenses/{expenseId}/title",
            TelegramIds.Admin,
            JsonContent.Create("Updated by admin"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var db = CreateDbContext();
        Assert.Equal("Updated by admin", await db.Expenses.Where(expense => expense.Id == expenseId).Select(expense => expense.Title).SingleAsync());
    }

    [Fact]
    public async Task UpdateExpense_WithExpenseFromAnotherGroupReturnsNotFound()
    {
        var firstGroup = await SeedGroupAsync();
        var secondGroup = await SeedGroupAsync(seedOffset: 100);
        var foreignExpenseId = await SeedExpenseAsync(
            secondGroup.GroupId,
            secondGroup.UserIds[TelegramIds.Owner + 100],
            secondGroup.UserIds[TelegramIds.Owner + 100]);

        using var response = await SendAuthenticatedAsync(
            HttpMethod.Put,
            $"/api/groups/{firstGroup.GroupId}/expenses/{foreignExpenseId}/title",
            TelegramIds.Member,
            JsonContent.Create("Cross-group mutation"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteGroup_AdminIsForbiddenAndOwnerCanDelete()
    {
        var data = await SeedGroupAsync();

        using var adminResponse = await SendAuthenticatedAsync(HttpMethod.Delete, $"/api/groups/{data.GroupId}", TelegramIds.Admin);
        using var ownerResponse = await SendAuthenticatedAsync(HttpMethod.Delete, $"/api/groups/{data.GroupId}", TelegramIds.Owner);

        Assert.Equal(HttpStatusCode.Forbidden, adminResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
    }

    [Fact]
    public async Task TransferOwnership_ReplacesRoleSetsForPreviousAndNewOwner()
    {
        var data = await SeedGroupAsync();
        var newOwnerId = data.UserIds[TelegramIds.Admin];

        using var response = await SendAuthenticatedAsync(
            HttpMethod.Post,
            $"/api/groups/{data.GroupId}/ownership",
            TelegramIds.Owner,
            JsonContent.Create(new TransferGroupOwnershipRequestDto { NewOwnerUserId = newOwnerId }));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var db = CreateDbContext();
        var group = await db.Groups.SingleAsync(group => group.Id == data.GroupId);
        var previousOwnerPermissions = await db.GroupMemberPermissions
            .Where(permission => permission.GroupId == data.GroupId && permission.UserId == data.UserIds[TelegramIds.Owner])
            .Select(permission => permission.Permission)
            .ToHashSetAsync();
        var newOwnerPermissions = await db.GroupMemberPermissions
            .Where(permission => permission.GroupId == data.GroupId && permission.UserId == newOwnerId)
            .Select(permission => permission.Permission)
            .ToHashSetAsync();

        Assert.Equal(newOwnerId, group.OwnerId);
        Assert.True(previousOwnerPermissions.SetEquals(GroupRolePresets.GetPermissions(GroupRole.Admin)));
        Assert.True(newOwnerPermissions.SetEquals(GroupRolePresets.GetPermissions(GroupRole.Owner)));
    }

    [Fact]
    public async Task RemoveUser_CannotRemoveCurrentOwner()
    {
        var data = await SeedGroupAsync();

        using var response = await SendAuthenticatedAsync(
            HttpMethod.Delete,
            $"/api/groups/{data.GroupId}/users/{data.UserIds[TelegramIds.Owner]}",
            TelegramIds.Admin);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMemberPermissions_CannotGrantOwnerOnlyPermission()
    {
        var data = await SeedGroupAsync();
        var requestDto = new UpdateGroupMemberPermissionsRequestDto
        {
            Role = GroupRole.Custom,
            Permissions = new[]
            {
                GroupPermission.ViewGroup,
                GroupPermission.DeleteGroup
            }
        };

        using var response = await SendAuthenticatedAsync(
            HttpMethod.Put,
            $"/api/groups/{data.GroupId}/users/{data.UserIds[TelegramIds.Member]}/permissions",
            TelegramIds.Owner,
            JsonContent.Create(requestDto));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<SeededGroup> SeedGroupAsync(long seedOffset = 0)
    {
        var users = new[]
        {
            CreateUser(TelegramIds.Owner + seedOffset, $"Owner {seedOffset}"),
            CreateUser(TelegramIds.Admin + seedOffset, $"Admin {seedOffset}"),
            CreateUser(TelegramIds.Member + seedOffset, $"Member {seedOffset}"),
            CreateUser(TelegramIds.Viewer + seedOffset, $"Viewer {seedOffset}")
        };
        var group = new Group
        {
            Title = $"Test group {seedOffset}",
            CreatedById = users[0].Id,
            OwnerId = users[0].Id
        };

        await using var db = CreateDbContext();
        db.Users.AddRange(users);
        db.Groups.Add(group);
        AddMembership(db, group.Id, users[0].Id, GroupRole.Owner);
        AddMembership(db, group.Id, users[1].Id, GroupRole.Admin);
        AddMembership(db, group.Id, users[2].Id, GroupRole.Member);
        AddMembership(db, group.Id, users[3].Id, GroupRole.Viewer);
        await db.SaveChangesAsync();

        return new SeededGroup(
            group.Id,
            users.ToDictionary(user => user.TelegramId, user => user.Id));
    }

    private async Task<Guid> SeedExpenseAsync(Guid groupId, Guid payerId, Guid createdByUserId)
    {
        var expense = new Expense
        {
            GroupId = groupId,
            PayerId = payerId,
            CreatedByUserId = createdByUserId,
            Title = "Original title",
            TotalAmount = 100,
            IsDraft = false
        };

        await using var db = CreateDbContext();
        db.Expenses.Add(expense);
        await db.SaveChangesAsync();
        return expense.Id;
    }

    private async Task<HttpResponseMessage> SendAuthenticatedAsync(HttpMethod method, string uri, long telegramId, HttpContent? content = null)
    {
        using var request = new HttpRequestMessage(method, uri) { Content = content };
        request.Headers.Add(TelegramAuthDefaults.InitDataHeaderName, TelegramInitDataBuilder.Create(telegramId));
        return await Client.SendAsync(request);
    }

    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(Database.ConnectionString)
            .Options;
        return new AppDbContext(options);
    }

    private static User CreateUser(long telegramId, string displayName) => new()
    {
        TelegramId = telegramId,
        DisplayName = displayName
    };

    private static void AddMembership(AppDbContext db, Guid groupId, Guid userId, GroupRole role)
    {
        db.UserGroups.Add(new UserGroup { GroupId = groupId, UserId = userId });
        db.GroupMemberPermissions.AddRange(GroupRolePresets.GetPermissions(role).Select(permission => new GroupMemberPermission
        {
            GroupId = groupId,
            UserId = userId,
            Permission = permission
        }));
    }

    private sealed record SeededGroup(Guid GroupId, IReadOnlyDictionary<long, Guid> UserIds);

    private static class TelegramIds
    {
        public const long Owner = 100_001;
        public const long Admin = 100_002;
        public const long Member = 100_003;
        public const long Viewer = 100_004;
        public const long NonMember = 100_005;
    }
}
