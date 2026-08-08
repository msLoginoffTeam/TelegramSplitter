using System.Net;
using System.Net.Http.Json;
using BudgetSplitter.App.Authentication;
using BudgetSplitter.App.IntegrationTests.Infrastructure;
using BudgetSplitter.Common.Dtos;
using BudgetSplitter.Common.Dtos.Request;
using BudgetSplitter.Common.Dtos.Response;
using BudgetSplitter.Tests.Shared;
using Microsoft.EntityFrameworkCore;

namespace BudgetSplitter.App.IntegrationTests.Operations;

public sealed class ExpenseAndPaymentTests(PostgreSqlFixture database) : IntegrationTestBase(database)
{
    [Fact]
    public async Task CreateExpense_StoresAuthorPayerAndBalancedShares()
    {
        var data = await GroupTestData.SeedGroupAsync(Database);
        var ownerId = data.UserIds[GroupTestTelegramIds.Owner];
        var memberId = data.UserIds[GroupTestTelegramIds.Member];
        var requestDto = new CreateExpenseRequestDto
        {
            Title = "Dinner",
            TotalAmount = 100,
            PayerId = ownerId,
            Shares = [new ExpenseShareCreateDto { UserId = memberId, Amount = 30 }]
        };

        using var response = await SendAuthenticatedAsync(
            HttpMethod.Post,
            $"/api/groups/{data.GroupId}/expenses",
            GroupTestTelegramIds.Member,
            JsonContent.Create(requestDto));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var db = GroupTestData.CreateDbContext(Database);
        var expense = await db.Expenses.Include(expense => expense.Shares).SingleAsync();
        Assert.Equal(memberId, expense.CreatedByUserId);
        Assert.Equal(ownerId, expense.PayerId);
        Assert.Equal(100, expense.Shares.Sum(share => share.Amount));
        Assert.Contains(expense.Shares, share => share.UserId == ownerId && share.Amount == 70);
        Assert.Contains(expense.Shares, share => share.UserId == memberId && share.Amount == 30);
    }

    [Fact]
    public async Task CreateExpense_RejectsInvalidParticipantsAndDoesNotPersistAnything()
    {
        var data = await GroupTestData.SeedGroupAsync(Database);
        var ownerId = data.UserIds[GroupTestTelegramIds.Owner];
        var memberId = data.UserIds[GroupTestTelegramIds.Member];
        var requestDto = new CreateExpenseRequestDto
        {
            Title = "Invalid expense",
            TotalAmount = 100,
            PayerId = ownerId,
            Shares =
            [
                new ExpenseShareCreateDto { UserId = memberId, Amount = 20 },
                new ExpenseShareCreateDto { UserId = memberId, Amount = 20 }
            ]
        };

        using var response = await SendAuthenticatedAsync(
            HttpMethod.Post,
            $"/api/groups/{data.GroupId}/expenses",
            GroupTestTelegramIds.Member,
            JsonContent.Create(requestDto));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var db = GroupTestData.CreateDbContext(Database);
        Assert.Equal(0, await db.Expenses.CountAsync());
        Assert.Equal(0, await db.ExpenseShares.CountAsync());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateExpense_RejectsNonPositiveTotal(decimal totalAmount)
    {
        var data = await GroupTestData.SeedGroupAsync(Database);
        var requestDto = new CreateExpenseRequestDto
        {
            Title = "Invalid total",
            TotalAmount = totalAmount,
            PayerId = data.UserIds[GroupTestTelegramIds.Owner]
        };

        using var response = await SendAuthenticatedAsync(
            HttpMethod.Post,
            $"/api/groups/{data.GroupId}/expenses",
            GroupTestTelegramIds.Member,
            JsonContent.Create(requestDto));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ExpenseParticipants_CannotModifyOrRemovePayerShare()
    {
        var data = await GroupTestData.SeedGroupAsync(Database);
        var ownerId = data.UserIds[GroupTestTelegramIds.Owner];
        var memberId = data.UserIds[GroupTestTelegramIds.Member];
        var expenseId = await GroupTestData.SeedExpenseAsync(
            Database,
            data.GroupId,
            ownerId,
            memberId,
            [(ownerId, 70), (memberId, 30)]);

        using var updateResponse = await SendAuthenticatedAsync(
            HttpMethod.Put,
            $"/api/groups/{data.GroupId}/expenses/{expenseId}/participants",
            GroupTestTelegramIds.Member,
            JsonContent.Create(new ExpenseShareCreateDto { UserId = ownerId, Amount = 20 }));
        using var removeResponse = await SendAuthenticatedAsync(
            HttpMethod.Delete,
            $"/api/groups/{data.GroupId}/expenses/{expenseId}/participants/{ownerId}",
            GroupTestTelegramIds.Member);

        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, removeResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteExpense_MemberCanDeleteOwnButCannotDeleteAnotherUsersExpense()
    {
        var data = await GroupTestData.SeedGroupAsync(Database);
        var ownerId = data.UserIds[GroupTestTelegramIds.Owner];
        var memberId = data.UserIds[GroupTestTelegramIds.Member];
        var ownExpenseId = await GroupTestData.SeedExpenseAsync(Database, data.GroupId, ownerId, memberId);
        var foreignExpenseId = await GroupTestData.SeedExpenseAsync(Database, data.GroupId, ownerId, ownerId);

        using var ownResponse = await SendAuthenticatedAsync(
            HttpMethod.Delete,
            $"/api/groups/{data.GroupId}/expenses/{ownExpenseId}",
            GroupTestTelegramIds.Member);
        using var foreignResponse = await SendAuthenticatedAsync(
            HttpMethod.Delete,
            $"/api/groups/{data.GroupId}/expenses/{foreignExpenseId}",
            GroupTestTelegramIds.Member);

        Assert.Equal(HttpStatusCode.OK, ownResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, foreignResponse.StatusCode);
    }

    [Fact]
    public async Task DirectPayment_RejectsSelfTransferAndNonPositiveAmount()
    {
        var data = await GroupTestData.SeedGroupAsync(Database);
        var ownerId = data.UserIds[GroupTestTelegramIds.Owner];

        using var selfResponse = await SendAuthenticatedAsync(
            HttpMethod.Post,
            $"/api/groups/{data.GroupId}/payments/direct",
            GroupTestTelegramIds.Member,
            JsonContent.Create(new CreateDirectPaymentRequestDto { FromUserId = ownerId, ToUserId = ownerId, Amount = 10 }));
        using var zeroResponse = await SendAuthenticatedAsync(
            HttpMethod.Post,
            $"/api/groups/{data.GroupId}/payments/direct",
            GroupTestTelegramIds.Member,
            JsonContent.Create(new CreateDirectPaymentRequestDto
            {
                FromUserId = ownerId,
                ToUserId = data.UserIds[GroupTestTelegramIds.Admin],
                Amount = 0
            }));

        Assert.Equal(HttpStatusCode.BadRequest, selfResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, zeroResponse.StatusCode);
    }

    [Fact]
    public async Task DirectPayment_AuthorCanUpdateOwnWhileAdminCanUpdateAny()
    {
        var data = await GroupTestData.SeedGroupAsync(Database);
        var requestDto = new CreateDirectPaymentRequestDto
        {
            FromUserId = data.UserIds[GroupTestTelegramIds.Owner],
            ToUserId = data.UserIds[GroupTestTelegramIds.Admin],
            Amount = 10
        };

        using var createResponse = await SendAuthenticatedAsync(
            HttpMethod.Post,
            $"/api/groups/{data.GroupId}/payments/direct",
            GroupTestTelegramIds.Member,
            JsonContent.Create(requestDto));
        var payment = await createResponse.Content.ReadFromJsonAsync<PaymentResponseDto>();
        Assert.NotNull(payment);

        using var authorUpdateResponse = await SendAuthenticatedAsync(
            HttpMethod.Put,
            $"/api/groups/{data.GroupId}/payments/{payment.Id}",
            GroupTestTelegramIds.Member,
            JsonContent.Create(new UpdatePaymentRequestDto { Amount = 15 }));
        using var adminUpdateResponse = await SendAuthenticatedAsync(
            HttpMethod.Put,
            $"/api/groups/{data.GroupId}/payments/{payment.Id}",
            GroupTestTelegramIds.Admin,
            JsonContent.Create(new UpdatePaymentRequestDto { Amount = 20 }));

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, authorUpdateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, adminUpdateResponse.StatusCode);
        await using var db = GroupTestData.CreateDbContext(Database);
        Assert.Equal(20, await db.Payments.Where(existingPayment => existingPayment.Id == payment.Id).Select(existingPayment => existingPayment.Amount).SingleAsync());
    }

    [Fact]
    public async Task ExpensePayment_UpdateAndDeleteKeepSharePaidStateInSync()
    {
        var data = await GroupTestData.SeedGroupAsync(Database);
        var ownerId = data.UserIds[GroupTestTelegramIds.Owner];
        var memberId = data.UserIds[GroupTestTelegramIds.Member];
        var expenseId = await GroupTestData.SeedExpenseAsync(
            Database,
            data.GroupId,
            ownerId,
            ownerId,
            [(ownerId, 70), (memberId, 30)]);

        using var createResponse = await SendAuthenticatedAsync(
            HttpMethod.Post,
            $"/api/groups/{data.GroupId}/payments/expense",
            GroupTestTelegramIds.Member,
            JsonContent.Create(new CreatePaymentForExpenseRequestDto { ExpenseId = expenseId, FromUserId = memberId, Amount = 30 }));
        var payment = await createResponse.Content.ReadFromJsonAsync<PaymentResponseDto>();
        Assert.NotNull(payment);
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        Assert.True(await GetShareIsPaidAsync(data.GroupId, expenseId, memberId));

        using var updateResponse = await SendAuthenticatedAsync(
            HttpMethod.Put,
            $"/api/groups/{data.GroupId}/payments/{payment.Id}",
            GroupTestTelegramIds.Member,
            JsonContent.Create(new UpdatePaymentRequestDto { Amount = 20 }));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.False(await GetShareIsPaidAsync(data.GroupId, expenseId, memberId));

        using var deleteResponse = await SendAuthenticatedAsync(
            HttpMethod.Delete,
            $"/api/groups/{data.GroupId}/payments/{payment.Id}",
            GroupTestTelegramIds.Member);
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        Assert.False(await GetShareIsPaidAsync(data.GroupId, expenseId, memberId));
    }

    [Fact]
    public async Task ManualSettlement_ChangesOnlyShareStatusAndCanBeReverted()
    {
        var data = await GroupTestData.SeedGroupAsync(Database);
        var ownerId = data.UserIds[GroupTestTelegramIds.Owner];
        var memberId = data.UserIds[GroupTestTelegramIds.Member];
        var expenseId = await GroupTestData.SeedExpenseAsync(
            Database,
            data.GroupId,
            ownerId,
            ownerId,
            [(ownerId, 70), (memberId, 30)]);
        var balancesBefore = await GetBalancesAsync(data.GroupId);

        using var settleResponse = await SendAuthenticatedAsync(
            HttpMethod.Put,
            $"/api/groups/{data.GroupId}/expenses/{expenseId}/participants/{memberId}/settlement",
            GroupTestTelegramIds.Owner,
            JsonContent.Create(new UpdateExpenseShareSettlementRequestDto { IsManuallySettled = true }));
        var settledExpense = await settleResponse.Content.ReadFromJsonAsync<ExpenseResponseDto>();

        Assert.Equal(HttpStatusCode.OK, settleResponse.StatusCode);
        Assert.NotNull(settledExpense);
        var settledShare = settledExpense.Shares.Single(share => share.UserId == memberId);
        Assert.True(settledShare.IsPaid);
        Assert.False(settledShare.IsPaidByPayments);
        Assert.True(settledShare.IsManuallySettled);
        Assert.Equal(balancesBefore, await GetBalancesAsync(data.GroupId));

        using var revertResponse = await SendAuthenticatedAsync(
            HttpMethod.Put,
            $"/api/groups/{data.GroupId}/expenses/{expenseId}/participants/{memberId}/settlement",
            GroupTestTelegramIds.Owner,
            JsonContent.Create(new UpdateExpenseShareSettlementRequestDto { IsManuallySettled = false }));
        var revertedExpense = await revertResponse.Content.ReadFromJsonAsync<ExpenseResponseDto>();

        Assert.Equal(HttpStatusCode.OK, revertResponse.StatusCode);
        Assert.NotNull(revertedExpense);
        var revertedShare = revertedExpense.Shares.Single(share => share.UserId == memberId);
        Assert.False(revertedShare.IsPaid);
        Assert.False(revertedShare.IsPaidByPayments);
        Assert.False(revertedShare.IsManuallySettled);
    }

    [Fact]
    public async Task ManualSettlement_RejectsShareAlreadyPaidByExpensePayments()
    {
        var data = await GroupTestData.SeedGroupAsync(Database);
        var ownerId = data.UserIds[GroupTestTelegramIds.Owner];
        var memberId = data.UserIds[GroupTestTelegramIds.Member];
        var expenseId = await GroupTestData.SeedExpenseAsync(
            Database,
            data.GroupId,
            ownerId,
            ownerId,
            [(ownerId, 70), (memberId, 30)]);

        using var paymentResponse = await SendAuthenticatedAsync(
            HttpMethod.Post,
            $"/api/groups/{data.GroupId}/payments/expense",
            GroupTestTelegramIds.Member,
            JsonContent.Create(new CreatePaymentForExpenseRequestDto
            {
                ExpenseId = expenseId,
                FromUserId = memberId,
                Amount = 30
            }));
        Assert.Equal(HttpStatusCode.OK, paymentResponse.StatusCode);

        using var settlementResponse = await SendAuthenticatedAsync(
            HttpMethod.Put,
            $"/api/groups/{data.GroupId}/expenses/{expenseId}/participants/{memberId}/settlement",
            GroupTestTelegramIds.Owner,
            JsonContent.Create(new UpdateExpenseShareSettlementRequestDto { IsManuallySettled = false }));

        Assert.Equal(HttpStatusCode.BadRequest, settlementResponse.StatusCode);
    }

    private async Task<bool> GetShareIsPaidAsync(Guid groupId, Guid expenseId, Guid userId)
    {
        using var response = await SendAuthenticatedAsync(
            HttpMethod.Get,
            $"/api/groups/{groupId}/expenses/{expenseId}",
            GroupTestTelegramIds.Member);
        var expense = await response.Content.ReadFromJsonAsync<ExpenseResponseDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(expense);
        return expense.Shares.Single(share => share.UserId == userId).IsPaid;
    }

    private async Task<IReadOnlyList<(Guid UserId, decimal Balance)>> GetBalancesAsync(Guid groupId)
    {
        using var response = await SendAuthenticatedAsync(
            HttpMethod.Get,
            $"/api/groups/{groupId}/balance",
            GroupTestTelegramIds.Member);
        var balance = await response.Content.ReadFromJsonAsync<BalanceResponseDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(balance);
        return balance.Balances
            .OrderBy(item => item.UserId)
            .Select(item => (item.UserId, item.Balance))
            .ToList();
    }

    private async Task<HttpResponseMessage> SendAuthenticatedAsync(HttpMethod method, string uri, long telegramId, HttpContent? content = null)
    {
        using var request = new HttpRequestMessage(method, uri) { Content = content };
        request.Headers.Add(TelegramAuthDefaults.InitDataHeaderName, TelegramInitDataBuilder.Create(telegramId));
        return await Client.SendAsync(request);
    }
}
