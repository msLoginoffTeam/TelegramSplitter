using Microsoft.EntityFrameworkCore;
using Persistence;

namespace BudgetSplitter.App.Infrastructure.Database;

public static class DbTransactionExtensions
{
    public static Task ExecuteInTransactionAsync(this AppDbContext db, Func<Task> operation)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            await operation();
            await transaction.CommitAsync();
        });
    }

    public static Task<TResult> ExecuteInTransactionAsync<TResult>(
        this AppDbContext db,
        Func<Task<TResult>> operation)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            var result = await operation();
            await transaction.CommitAsync();
            return result;
        });
    }
}
