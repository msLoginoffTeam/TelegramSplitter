using System.Net;
using System.Net.Http.Json;
using BudgetSplitter.App.Authentication;
using BudgetSplitter.App.IntegrationTests.Infrastructure;
using BudgetSplitter.Common.Dtos.Response;
using BudgetSplitter.Tests.Shared;

namespace BudgetSplitter.App.IntegrationTests.Balances;

public sealed class BalanceAndTransferTests(PostgreSqlFixture database) : IntegrationTestBase(database)
{
    [Fact]
    public async Task BalancesAndTransfers_ReflectExpenseSharesAndSettleEveryDebt()
    {
        var data = await GroupTestData.SeedGroupAsync(Database);
        var ownerId = data.UserIds[GroupTestTelegramIds.Owner];
        var adminId = data.UserIds[GroupTestTelegramIds.Admin];
        var memberId = data.UserIds[GroupTestTelegramIds.Member];
        var viewerId = data.UserIds[GroupTestTelegramIds.Viewer];
        await GroupTestData.SeedExpenseAsync(
            Database,
            data.GroupId,
            ownerId,
            ownerId,
            [(ownerId, 40), (adminId, 30), (memberId, 20), (viewerId, 10)]);

        using var balanceResponse = await SendAuthenticatedAsync(
            HttpMethod.Get,
            $"/api/groups/{data.GroupId}/balance",
            GroupTestTelegramIds.Viewer);
        using var transfersResponse = await SendAuthenticatedAsync(
            HttpMethod.Get,
            $"/api/groups/{data.GroupId}/transfers",
            GroupTestTelegramIds.Viewer);
        var balances = await balanceResponse.Content.ReadFromJsonAsync<BalanceResponseDto>();
        var transfers = await transfersResponse.Content.ReadFromJsonAsync<TransferSuggestionsResponseDto>();

        Assert.Equal(HttpStatusCode.OK, balanceResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, transfersResponse.StatusCode);
        Assert.NotNull(balances);
        Assert.NotNull(transfers);

        var currentBalances = balances.Balances.ToDictionary(balance => balance.UserId, balance => balance.Balance);
        Assert.Equal(60, currentBalances[ownerId]);
        Assert.Equal(-30, currentBalances[adminId]);
        Assert.Equal(-20, currentBalances[memberId]);
        Assert.Equal(-10, currentBalances[viewerId]);

        Assert.All(transfers.Transfers, transfer =>
        {
            Assert.True(transfer.Amount > 0);
            Assert.True(currentBalances[transfer.FromUserId] < 0);
            Assert.True(currentBalances[transfer.ToUserId] > 0);
        });
        Assert.DoesNotContain(
            transfers.Transfers,
            first => transfers.Transfers.Any(second =>
                first.FromUserId == second.ToUserId && first.ToUserId == second.FromUserId));

        foreach (var transfer in transfers.Transfers)
        {
            currentBalances[transfer.FromUserId] += transfer.Amount;
            currentBalances[transfer.ToUserId] -= transfer.Amount;
        }

        Assert.All(currentBalances.Values, balance => Assert.Equal(0, balance));
    }

    private async Task<HttpResponseMessage> SendAuthenticatedAsync(HttpMethod method, string uri, long telegramId)
    {
        using var request = new HttpRequestMessage(method, uri);
        request.Headers.Add(TelegramAuthDefaults.InitDataHeaderName, TelegramInitDataBuilder.Create(telegramId));
        return await Client.SendAsync(request);
    }
}
