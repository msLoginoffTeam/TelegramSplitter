using BudgetSplitter.Common.Authorization;
using Persistence;

namespace BudgetSplitter.App.Authorization;

public interface IGroupAuthorizationService
{
    Task EnsurePermissionAsync(Guid groupId, GroupPermission permission, CancellationToken cancellationToken = default);
    Task EnsureOwnerAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task<UserGroup> GetMembershipAsync(Guid groupId, CancellationToken cancellationToken = default);
}
