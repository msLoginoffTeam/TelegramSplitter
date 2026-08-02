using BudgetSplitter.Common.Authorization;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace BudgetSplitter.App.IntegrationTests.Infrastructure;

public sealed record SeededGroup(Guid GroupId, IReadOnlyDictionary<long, Guid> UserIds);

public static class GroupTestData
{
    public static async Task<SeededGroup> SeedGroupAsync(PostgreSqlFixture database, long seedOffset = 0)
    {
        var users = new[]
        {
            CreateUser(GroupTestTelegramIds.Owner + seedOffset, $"Owner {seedOffset}"),
            CreateUser(GroupTestTelegramIds.Admin + seedOffset, $"Admin {seedOffset}"),
            CreateUser(GroupTestTelegramIds.Member + seedOffset, $"Member {seedOffset}"),
            CreateUser(GroupTestTelegramIds.Viewer + seedOffset, $"Viewer {seedOffset}")
        };
        var group = new Group
        {
            Title = $"Test group {seedOffset}",
            CreatedById = users[0].Id,
            OwnerId = users[0].Id
        };

        await using var db = CreateDbContext(database);
        db.Users.AddRange(users);
        db.Groups.Add(group);
        AddMembership(db, group.Id, users[0].Id, GroupRole.Owner);
        AddMembership(db, group.Id, users[1].Id, GroupRole.Admin);
        AddMembership(db, group.Id, users[2].Id, GroupRole.Member);
        AddMembership(db, group.Id, users[3].Id, GroupRole.Viewer);
        await db.SaveChangesAsync();

        return new SeededGroup(group.Id, users.ToDictionary(user => user.TelegramId, user => user.Id));
    }

    public static async Task<Guid> SeedExpenseAsync(
        PostgreSqlFixture database,
        Guid groupId,
        Guid payerId,
        Guid createdByUserId,
        IEnumerable<(Guid UserId, decimal Amount)>? shares = null)
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

        await using var db = CreateDbContext(database);
        db.Expenses.Add(expense);
        if (shares is not null)
        {
            foreach (var (userId, amount) in shares)
            {
                db.ExpenseShares.Add(new ExpenseShare
                {
                    ExpenseId = expense.Id,
                    UserId = userId,
                    Amount = amount
                });
            }
        }

        await db.SaveChangesAsync();
        return expense.Id;
    }

    public static AppDbContext CreateDbContext(PostgreSqlFixture database)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(database.ConnectionString)
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
}

public static class GroupTestTelegramIds
{
    public const long Owner = 100_001;
    public const long Admin = 100_002;
    public const long Member = 100_003;
    public const long Viewer = 100_004;
    public const long NonMember = 100_005;
}
