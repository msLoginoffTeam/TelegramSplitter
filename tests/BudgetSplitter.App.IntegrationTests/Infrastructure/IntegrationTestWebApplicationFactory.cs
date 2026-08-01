using BudgetSplitter.App.Authentication;
using BudgetSplitter.Tests.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Persistence;

namespace BudgetSplitter.App.IntegrationTests.Infrastructure;

public sealed class IntegrationTestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly PostgreSqlFixture _database;
    private readonly string _environment;

    public IntegrationTestWebApplicationFactory(PostgreSqlFixture database, string environment = "Production")
    {
        _database = database;
        _environment = environment;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environment);
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _database.ConnectionString
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseNpgsql(_database.ConnectionString));
            services.PostConfigure<TelegramAuthOptions>(options =>
            {
                options.BotToken = TelegramInitDataBuilder.BotToken;
                options.MaxAuthAgeSeconds = 3_600;
            });
        });
    }
}
