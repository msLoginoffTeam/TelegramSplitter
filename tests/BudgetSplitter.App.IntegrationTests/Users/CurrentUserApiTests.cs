using System.Net;
using System.Net.Http.Json;
using BudgetSplitter.App.Authentication;
using BudgetSplitter.App.IntegrationTests.Infrastructure;
using BudgetSplitter.Common.Dtos.Response;
using BudgetSplitter.Tests.Shared;

namespace BudgetSplitter.App.IntegrationTests.Users;

public sealed class CurrentUserApiTests(PostgreSqlFixture database) : IntegrationTestBase(database)
{
    [Fact]
    public async Task GetMe_AutoProvisionsAndReturnsOnlyAuthenticatedUser()
    {
        const long telegramId = 201_001;

        using var response = await SendAuthenticatedAsync(HttpMethod.Get, "/api/users/me", telegramId);
        var profile = await response.Content.ReadFromJsonAsync<UserResponseDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(profile);
        Assert.Equal(telegramId, profile.TelegramId);
        Assert.NotEqual(Guid.Empty, profile.Id);
    }

    [Fact]
    public async Task GetMe_UsesProfileFromTelegramInitData()
    {
        const long telegramId = 201_002;

        using var response = await SendAuthenticatedAsync(HttpMethod.Get, "/api/users/me", telegramId);
        var profile = await response.Content.ReadFromJsonAsync<UserResponseDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(profile);
        Assert.Equal("Test", profile.DisplayName);
    }

    [Theory]
    [InlineData("GET", "/api/users")]
    [InlineData("GET", "/api/users/find?userTelegramId=201003")]
    [InlineData("POST", "/api/users")]
    [InlineData("PUT", "/api/users/00000000-0000-0000-0000-000000000001")]
    public async Task LegacyUserManagementRoutes_AreNotExposed(string method, string uri)
    {
        using var response = await SendAuthenticatedAsync(new HttpMethod(method), uri, 201_003);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<HttpResponseMessage> SendAuthenticatedAsync(HttpMethod method, string uri, long telegramId, HttpContent? content = null)
    {
        using var request = new HttpRequestMessage(method, uri) { Content = content };
        request.Headers.Add(TelegramAuthDefaults.InitDataHeaderName, TelegramInitDataBuilder.Create(telegramId));
        return await Client.SendAsync(request);
    }
}
