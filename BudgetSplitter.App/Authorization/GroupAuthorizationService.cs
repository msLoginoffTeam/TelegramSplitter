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

    public async Task<UserGroup> GetMembershipAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var user = await currentUser.GetRequiredUserAsync(cancellationToken);
        return await db.UserGroups
                   .Include(membership => membership.Permissions)
                   .SingleOrDefaultAsync(membership => membership.GroupId == groupId && membership.UserId == user.Id, cancellationToken)
               ?? throw new ForbiddenException("You are not a member of this group.");
    }
}
