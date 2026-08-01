using Microsoft.AspNetCore.Mvc.Testing;

namespace BudgetSplitter.App.IntegrationTests.Infrastructure;

[Collection(IntegrationTestCollection.Name)]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly IntegrationTestWebApplicationFactory _factory;

    protected IntegrationTestBase(PostgreSqlFixture database)
    {
        Database = database;
        _factory = new IntegrationTestWebApplicationFactory(database);
        Client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    protected PostgreSqlFixture Database { get; }
    protected HttpClient Client { get; }

    public Task InitializeAsync() => Database.ResetDatabaseAsync();

    public Task DisposeAsync()
    {
        Client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }
}
