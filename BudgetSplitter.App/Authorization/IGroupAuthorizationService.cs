using BudgetSplitter.Common.Authorization;
using Persistence;

namespace BudgetSplitter.App.Authorization;

public interface IGroupAuthorizationService
{
    Task EnsurePermissionAsync(Guid groupId, GroupPermission permission, CancellationToken cancellationToken = default);
    Task EnsureOwnerAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task EnsureExpensePermissionAsync(Guid groupId, Guid expenseId, GroupPermission ownPermission, GroupPermission anyPermission, CancellationToken cancellationToken = default);
    Task EnsurePaymentPermissionAsync(Guid groupId, Guid paymentId, GroupPermission ownPermission, GroupPermission anyPermission, CancellationToken cancellationToken = default);
    Task<UserGroup> GetMembershipAsync(Guid groupId, CancellationToken cancellationToken = default);
}
