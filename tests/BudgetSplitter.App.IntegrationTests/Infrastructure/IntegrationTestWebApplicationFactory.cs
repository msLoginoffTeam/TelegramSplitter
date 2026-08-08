using AuditLogLens;
using BudgetSplitter.App.Authentication;
using BudgetSplitter.Tests.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Persistence;

namespace BudgetSplitter.App.IntegrationTests.Infrastructure;

public sealed class IntegrationTestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly PostgreSqlFixture _database;
    private readonly string _environment;

    public IntegrationTestWebApplicationFactory(PostgreSqlFixture database, string environment = "Tests")
    {
        _database = database;
        _environment = environment;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environment);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>((provider, options) =>
            {
                options.UseNpgsql(
                    _database.ConnectionString,
                    npgsql => npgsql.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null));
                options.AddAuditInterceptor(provider);
            });
            services.PostConfigure<TelegramAuthOptions>(options =>
            {
                options.BotToken = TelegramInitDataBuilder.BotToken;
                options.MaxAuthAgeSeconds = 3_600;
            });
        });
    }
}
