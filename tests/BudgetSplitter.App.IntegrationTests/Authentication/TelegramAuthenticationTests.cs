using System.Net;
using BudgetSplitter.App.Authentication;
using BudgetSplitter.App.IntegrationTests.Infrastructure;
using BudgetSplitter.Tests.Shared;
using Microsoft.Extensions.Hosting;

namespace BudgetSplitter.App.IntegrationTests.Authentication;

public sealed class TelegramAuthenticationTests(PostgreSqlFixture database) : IntegrationTestBase(database)
{
    [Fact]
    public async Task ApiRequest_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/groups?telegramChatId=0");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(response.Headers.WwwAuthenticate, header => header.Scheme == TelegramAuthDefaults.Scheme);
    }

    [Fact]
    public async Task ApiRequest_WithValidInitData_ReturnsSuccess()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/groups?telegramChatId=0");
        request.Headers.Add(TelegramAuthDefaults.InitDataHeaderName, TelegramInitDataBuilder.Create());

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProductionApiRequest_WithDevelopmentHeader_ReturnsUnauthorized()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/groups?telegramChatId=0");
        request.Headers.Add(TelegramAuthDefaults.DevelopmentUserIdHeaderName, "123456789");

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DevelopmentApiRequest_WithDevelopmentHeader_ReturnsSuccess()
    {
        using var factory = new IntegrationTestWebApplicationFactory(Database, Environments.Development);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/groups?telegramChatId=0");
        request.Headers.Add(TelegramAuthDefaults.DevelopmentUserIdHeaderName, "123456789");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
