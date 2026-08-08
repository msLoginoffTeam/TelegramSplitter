using System.Net;
using System.Net.Http.Json;
using BudgetSplitter.App.Authentication;
using BudgetSplitter.App.IntegrationTests.Infrastructure;
using BudgetSplitter.Common.Dtos.Request;
using BudgetSplitter.Common.Dtos.Response;
using BudgetSplitter.Tests.Shared;
using Microsoft.EntityFrameworkCore;

namespace BudgetSplitter.App.IntegrationTests.Audit;

public sealed class AuditLogTests(PostgreSqlFixture database) : IntegrationTestBase(database)
{
    [Fact]
    public async Task CreateGroup_WritesGroupHistoryWithActorAndGroupId()
    {
        const long telegramId = 555_001;

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/groups")
        {
            Content = JsonContent.Create(new CreateGroupRequestDto { Title = "Audit trip" })
        };
        request.Headers.Add(TelegramAuthDefaults.InitDataHeaderName, TelegramInitDataBuilder.Create(telegramId));

        using var response = await Client.SendAsync(request);
        var createdGroup = await response.Content.ReadFromJsonAsync<GroupResponseDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(createdGroup);

        await using var db = GroupTestData.CreateDbContext(Database);
        var entries = await db.AuditLogEntries
            .Where(entry => entry.GroupId == createdGroup.Id)
            .ToListAsync();

        var groupEntry = Assert.Single(entries, entry => entry.SubjectType == "Group");
        Assert.Equal("Added", groupEntry.Operation);
        Assert.Equal(telegramId, groupEntry.ActorTelegramId);
        Assert.Contains("Audit trip", groupEntry.NewValuesJson);
        Assert.Contains("Members", groupEntry.NewValuesJson);
    }

    [Fact]
    public async Task RemoveMember_WritesMembersCollectionChangeForGroup()
    {
        var seededGroup = await GroupTestData.SeedGroupAsync(Database);
        var memberId = seededGroup.UserIds[GroupTestTelegramIds.Member];

        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/api/groups/{seededGroup.GroupId}/users/{memberId}");
        request.Headers.Add(
            TelegramAuthDefaults.InitDataHeaderName,
            TelegramInitDataBuilder.Create(GroupTestTelegramIds.Owner));

        using var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var db = GroupTestData.CreateDbContext(Database);
        var entry = await db.AuditLogEntries.SingleAsync(auditEntry =>
            auditEntry.GroupId == seededGroup.GroupId &&
            auditEntry.SubjectType == nameof(Persistence.Group) &&
            auditEntry.Operation == "Modified");

        Assert.Contains("Members", entry.OldValuesJson);
        Assert.Contains("Member 0", entry.OldValuesJson);
        Assert.Equal(GroupTestTelegramIds.Owner, entry.ActorTelegramId);
    }
}
