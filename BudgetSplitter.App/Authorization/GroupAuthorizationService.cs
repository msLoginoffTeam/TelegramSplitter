using BudgetSplitter.Common.Authorization;
using BudgetSplitter.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace BudgetSplitter.App.Authorization;

public sealed class GroupAuthorizationService(AppDbContext db, ICurrentUserService currentUser) : IGroupAuthorizationService
{
    public async Task EnsurePermissionAsync(Guid groupId, GroupPermission permission, CancellationToken cancellationToken = default)
    {
        var membership = await GetMembershipAsync(groupId, cancellationToken);
        if (membership.Permissions.All(grant => grant.Permission != permission))
        {
            throw new ForbiddenException($"Permission '{permission}' is required for this group.");
        }
    }

    public async Task EnsureOwnerAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var user = await currentUser.GetRequiredUserAsync(cancellationToken);
        var isOwner = await db.Groups.AnyAsync(group => group.Id == groupId && group.OwnerId == user.Id, cancellationToken);
        if (!isOwner) throw new ForbiddenException("Only the group owner can perform this action.");
    }

    public async Task EnsureExpensePermissionAsync(
        Guid groupId,
        Guid expenseId,
        GroupPermission ownPermission,
        GroupPermission anyPermission,
        CancellationToken cancellationToken = default)
    {
        var expense = await db.Expenses
                          .AsNoTracking()
                          .SingleOrDefaultAsync(expense => expense.Id == expenseId && expense.GroupId == groupId, cancellationToken)
                      ?? throw new NotFoundException($"Expense {expenseId} not found in group {groupId}.");

        await EnsureOperationPermissionAsync(
            expense.GroupId,
            expense.CreatedByUserId,
            ownPermission,
            anyPermission,
            cancellationToken);
    }

    public async Task EnsurePaymentPermissionAsync(
        Guid groupId,
        Guid paymentId,
        GroupPermission ownPermission,
        GroupPermission anyPermission,
        CancellationToken cancellationToken = default)
    {
        var payment = await db.Payments
                          .AsNoTracking()
                          .SingleOrDefaultAsync(payment => payment.Id == paymentId && payment.GroupId == groupId, cancellationToken)
                      ?? throw new NotFoundException($"Payment {paymentId} not found in group {groupId}.");

        await EnsureOperationPermissionAsync(
            payment.GroupId,
            payment.CreatedByUserId,
            ownPermission,
            anyPermission,
            cancellationToken);
    }

    public async Task<UserGroup> GetMembershipAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var user = await currentUser.GetRequiredUserAsync(cancellationToken);
        return await db.UserGroups
                   .Include(membership => membership.Permissions)
                   .SingleOrDefaultAsync(membership => membership.GroupId == groupId && membership.UserId == user.Id, cancellationToken)
               ?? throw new ForbiddenException("You are not a member of this group.");
    }

    private async Task EnsureOperationPermissionAsync(
        Guid groupId,
        Guid createdByUserId,
        GroupPermission ownPermission,
        GroupPermission anyPermission,
        CancellationToken cancellationToken)
    {
        var user = await currentUser.GetRequiredUserAsync(cancellationToken);
        var membership = await GetMembershipAsync(groupId, cancellationToken);
        var requiredPermission = user.Id == createdByUserId ? ownPermission : anyPermission;

        if (!membership.Permissions.Any(grant => grant.Permission == requiredPermission))
        {
            throw new ForbiddenException($"Permission '{requiredPermission}' is required for this operation.");
        }
    }
}
