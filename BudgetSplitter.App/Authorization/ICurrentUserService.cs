using Persistence;

namespace BudgetSplitter.App.Authorization;

public interface ICurrentUserService
{
    Task<User> GetRequiredUserAsync(CancellationToken cancellationToken = default);
}
