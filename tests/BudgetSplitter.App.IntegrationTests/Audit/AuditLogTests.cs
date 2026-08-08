using System.Net;
using System.Net.Http.Json;
using BudgetSplitter.App.Authentication;
using BudgetSplitter.App.IntegrationTests.Infrastructure;
using BudgetSplitter.Common.Dtos;
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

    [Fact]
    public async Task UpdateExpense_UpdatesExistingSharesAndWritesAmountChanges()
    {
        var seededGroup = await GroupTestData.SeedGroupAsync(Database);
        var ownerId = seededGroup.UserIds[GroupTestTelegramIds.Owner];
        var memberId = seededGroup.UserIds[GroupTestTelegramIds.Member];
        var expenseId = await GroupTestData.SeedExpenseAsync(
            Database,
            seededGroup.GroupId,
            ownerId,
            ownerId,
            [(ownerId, 70), (memberId, 30)]);

        Dictionary<Guid, Guid> before;
        await using (var db = GroupTestData.CreateDbContext(Database))
        {
            before = await db.ExpenseShares
                .Where(share => share.ExpenseId == expenseId)
                .ToDictionaryAsync(share => share.UserId, share => share.Id);
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"/api/groups/{seededGroup.GroupId}/expenses/{expenseId}")
        {
            Content = JsonContent.Create(new UpdateExpenseRequestDto
            {
                Title = "Updated expense",
                TotalAmount = 100,
                PayerId = ownerId,
                Shares = [new ExpenseShareCreateDto { UserId = memberId, Amount = 40 }]
            })
        };
        request.Headers.Add(
            TelegramAuthDefaults.InitDataHeaderName,
            TelegramInitDataBuilder.Create(GroupTestTelegramIds.Owner));

        using var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using (var db = GroupTestData.CreateDbContext(Database))
        {
            var after = await db.ExpenseShares
                .Where(share => share.ExpenseId == expenseId)
                .ToDictionaryAsync(share => share.UserId, share => new { share.Id, share.Amount });
            Assert.Equal(before[ownerId], after[ownerId].Id);
            Assert.Equal(before[memberId], after[memberId].Id);
            Assert.Equal(60, after[ownerId].Amount);
            Assert.Equal(40, after[memberId].Amount);
        }

        await using var auditDb = GroupTestData.CreateDbContext(Database);
        var shareEntries = await auditDb.AuditLogEntries
            .Where(entry => entry.GroupId == seededGroup.GroupId && entry.SubjectType == nameof(Persistence.ExpenseShare))
            .ToListAsync();

        Assert.Equal(2, shareEntries.Count);
        Assert.All(shareEntries, entry => Assert.Equal("Modified", entry.Operation));
        Assert.Contains(shareEntries, entry =>
            entry.OldValuesJson!.Contains("30") &&
            entry.NewValuesJson!.Contains("40") &&
            entry.NewValuesJson.Contains("Updated expense") &&
            entry.NewValuesJson.Contains("Member 0"));
    }

    [Fact]
    public async Task UpdatePayment_WritesReadableSenderAndRecipientContext()
    {
        var seededGroup = await GroupTestData.SeedGroupAsync(Database);
        var ownerId = seededGroup.UserIds[GroupTestTelegramIds.Owner];
        var memberId = seededGroup.UserIds[GroupTestTelegramIds.Member];

        using var createRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/groups/{seededGroup.GroupId}/payments/direct")
        {
            Content = JsonContent.Create(new CreateDirectPaymentRequestDto
            {
                FromUserId = ownerId,
                ToUserId = memberId,
                Amount = 10
            })
        };
        createRequest.Headers.Add(
            TelegramAuthDefaults.InitDataHeaderName,
            TelegramInitDataBuilder.Create(GroupTestTelegramIds.Owner));

        using var createResponse = await Client.SendAsync(createRequest);
        var payment = await createResponse.Content.ReadFromJsonAsync<PaymentResponseDto>();

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        Assert.NotNull(payment);

        using var updateRequest = new HttpRequestMessage(
            HttpMethod.Put,
            $"/api/groups/{seededGroup.GroupId}/payments/{payment.Id}")
        {
            Content = JsonContent.Create(new UpdatePaymentRequestDto { Amount = 15 })
        };
        updateRequest.Headers.Add(
            TelegramAuthDefaults.InitDataHeaderName,
            TelegramInitDataBuilder.Create(GroupTestTelegramIds.Owner));

        using var updateResponse = await Client.SendAsync(updateRequest);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        await using var db = GroupTestData.CreateDbContext(Database);
        var entry = await db.AuditLogEntries.SingleAsync(auditEntry =>
            auditEntry.GroupId == seededGroup.GroupId &&
            auditEntry.SubjectType == nameof(Persistence.Payment) &&
            auditEntry.Operation == "Modified");

        Assert.Contains("FromParticipant", entry.NewValuesJson);
        Assert.Contains("ToParticipant", entry.NewValuesJson);
        Assert.Contains("Test", entry.NewValuesJson);
    }
}
