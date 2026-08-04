using System.Net;
using System.Net.Http.Json;
using BudgetSplitter.App.Authentication;
using BudgetSplitter.App.IntegrationTests.Infrastructure;
using BudgetSplitter.Common.Dtos.Request;
using BudgetSplitter.Common.Dtos.Response;
using BudgetSplitter.Tests.Shared;

namespace BudgetSplitter.App.IntegrationTests.Groups;

public sealed class GroupCreationTests(PostgreSqlFixture database) : IntegrationTestBase(database)
{
    [Fact]
    public async Task CreateGroup_ReturnsCreatedGroupAndLocation()
    {
        const long telegramId = 202_001;
        const string title = "Weekend trip";

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/groups")
        {
            Content = JsonContent.Create(new CreateGroupRequestDto { Title = title })
        };
        request.Headers.Add(TelegramAuthDefaults.InitDataHeaderName, TelegramInitDataBuilder.Create(telegramId));

        using var response = await Client.SendAsync(request);
        var createdGroup = await response.Content.ReadFromJsonAsync<GroupResponseDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(createdGroup);
        Assert.NotEqual(Guid.Empty, createdGroup.Id);
        Assert.Equal(title, createdGroup.Title);
        Assert.EndsWith($"/api/groups/{createdGroup.Id}", response.Headers.Location?.ToString());
    }
}
