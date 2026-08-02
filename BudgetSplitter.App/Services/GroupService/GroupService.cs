using BudgetSplitter.Common.Dtos.Request;
using BudgetSplitter.Common.Dtos.Response;
using BudgetSplitter.Common.Exceptions;
using BudgetSplitter.Common.Authorization;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace BudgetSplitter.App.Services.GroupService;

public class GroupService : IGroupService
{
    private readonly AppDbContext _db;
    public GroupService(AppDbContext db) => _db = db;

    public async Task<IEnumerable<GroupOverviewResponseDto>> GetMyGroupsAsync(Guid userId)
    {
        var groups = await _db.Groups
            .Where(g => g.UserGroups.Any(ug => ug.UserId == userId))
            .AsNoTracking()
            .ToListAsync();

        return groups
            .Select(g => new GroupOverviewResponseDto
            {
                Id = g.Id,
                Title = g.Title
            });
    }

    public async Task<IEnumerable<GroupOverviewResponseDto>> GetGroupsAsync(long telegramChatId, Guid userId)
    {
        var groups = await _db.Groups
            .Where(g => g.TelegramChatId == telegramChatId && g.UserGroups.Any(ug => ug.UserId == userId))
            .AsNoTracking()
            .ToListAsync();
        
        return groups
            .Select(g => new GroupOverviewResponseDto
            {
                Id = g.Id,
                Title = g.Title
            });
    }

    public async Task<GroupResponseDto> GetGroupAsync(Guid groupId)
    {
        var group = await _db.Groups
            .Include(g => g.UserGroups)
            .ThenInclude(ug => ug.User)
            .Include(g => g.UserGroups)
            .ThenInclude(ug => ug.Permissions)
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
            throw new NotFoundException($"Group {groupId} not found");

        return new GroupResponseDto
        {
            Id = group.Id,
            Title = group.Title,
            TelegramChatId = group.TelegramChatId,
            Members = group.UserGroups
                .OrderBy(ug => ug.User.DisplayName)
                .Select(ug => new GroupMemberResponseDto
                {
                    UserId = ug.User.Id,
                    TelegramId = ug.User.TelegramId,
                    DisplayName = ug.User.DisplayName,
                    IsOwner = ug.UserId == group.OwnerId,
                    Role = GroupRolePresets.ResolveRole(
                        ug.Permissions.Select(permission => permission.Permission).ToHashSet(),
                        ug.UserId == group.OwnerId),
                    Permissions = ug.Permissions
                        .Select(permission => permission.Permission)
                        .Order()
                        .ToArray()
                })
                .ToList()
        };
    }

    public async Task<GroupResponseDto> CreateGroupAsync(CreateGroupRequestDto dto, User creator)
    {
        var group = new Group
        {
            Title = dto.Title,
            TelegramChatId = dto.TelegramChatId,
            CreatedById = creator.Id,
            OwnerId = creator.Id
        };
        _db.Groups.Add(group);
        _db.UserGroups.Add(new UserGroup { Group = group, User = creator });
        _db.GroupMemberPermissions.AddRange(GroupRolePresets.All.Select(permission => new GroupMemberPermission
        {
            GroupId = group.Id,
            UserId = creator.Id,
            Permission = permission
        }));
        await _db.SaveChangesAsync();
        
        return await GetGroupAsync(group.Id);
    }

    public async Task UpdateGroupAsync(Guid groupId, UpdateGroupRequestDto dto)
    {
        var group = await _db.Groups.FindAsync(groupId);
        if (group == null)
            throw new NotFoundException($"Group {groupId} not found");

        group.Title = dto.Title;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteGroupAsync(Guid groupId)
    {
        var group = await _db.Groups.FindAsync(groupId);
        if (group == null) return;
        _db.Groups.Remove(group);
        await _db.SaveChangesAsync();
    }

    public async Task AddUserAsync(Guid groupId, AddGroupUserRequestDto dto)
    {
        var group = await _db.Groups.FindAsync(groupId)
                    ?? throw new NotFoundException($"Group {groupId} not found");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.TelegramId == dto.TelegramId);
        if (user == null)
        {
            throw new NotFoundException($"User with telegramId {dto.TelegramId} not found");
        }

        if (!await _db.UserGroups.AnyAsync(ug => ug.GroupId == group.Id && ug.UserId == user.Id))
        {
            _db.UserGroups.Add(new UserGroup
            {
                Group = group,
                User = user
            });
            _db.GroupMemberPermissions.AddRange(GroupRolePresets.GetPermissions(GroupRole.Member)
                .Select(permission => new GroupMemberPermission
                {
                    GroupId = group.Id,
                    UserId = user.Id,
                    Permission = permission
                }));
        }
        else
        {
            throw new BadRequestException($"User with id {dto.TelegramId} already exists in group {groupId}");
        }

        await _db.SaveChangesAsync();
    }

    public async Task RemoveUserAsync(Guid groupId, Guid userId)
    {
        var isOwner = await _db.Groups
            .AnyAsync(group => group.Id == groupId && group.OwnerId == userId);
        if (isOwner)
        {
            throw new BadRequestException("Transfer ownership before removing the group owner.");
        }

        var ug = await _db.UserGroups
            .FirstOrDefaultAsync(x => x.GroupId == groupId && x.UserId == userId);
        if (ug != null)
        {
            _db.UserGroups.Remove(ug);
            await _db.SaveChangesAsync();
        }
        else
        {
            throw new BadRequestException($"User with id {userId} does not exist in group {groupId}");
        }
    }

    public async Task UpdateMemberPermissionsAsync(Guid groupId, Guid userId, UpdateGroupMemberPermissionsRequestDto dto)
    {
        var group = await _db.Groups.SingleOrDefaultAsync(group => group.Id == groupId)
                    ?? throw new NotFoundException($"Group {groupId} not found");
        if (group.OwnerId == userId)
        {
            throw new BadRequestException("Owner permissions can only change through ownership transfer.");
        }

        var membership = await _db.UserGroups
                             .Include(member => member.Permissions)
                             .SingleOrDefaultAsync(member => member.GroupId == groupId && member.UserId == userId)
                         ?? throw new NotFoundException($"User {userId} is not a member of group {groupId}");

        var permissions = ResolvePermissions(dto);
        membership.Permissions.Clear();
        foreach (var permission in permissions)
        {
            membership.Permissions.Add(new GroupMemberPermission
            {
                GroupId = groupId,
                UserId = userId,
                Permission = permission
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task TransferOwnershipAsync(Guid groupId, Guid newOwnerUserId)
    {
        var group = await _db.Groups.SingleOrDefaultAsync(group => group.Id == groupId)
                    ?? throw new NotFoundException($"Group {groupId} not found");
        if (group.OwnerId == newOwnerUserId) return;

        var newOwnerMembership = await _db.UserGroups
                                     .Include(member => member.Permissions)
                                     .SingleOrDefaultAsync(member => member.GroupId == groupId && member.UserId == newOwnerUserId)
                                 ?? throw new BadRequestException("New owner must be a group member.");
        var previousOwnerMembership = await _db.UserGroups
                                         .Include(member => member.Permissions)
                                         .SingleAsync(member => member.GroupId == groupId && member.UserId == group.OwnerId);

        ReplacePermissions(previousOwnerMembership, GroupRolePresets.GetPermissions(GroupRole.Admin));
        ReplacePermissions(newOwnerMembership, GroupRolePresets.GetPermissions(GroupRole.Owner));
        group.OwnerId = newOwnerUserId;
        await _db.SaveChangesAsync();
    }

    private static IReadOnlySet<GroupPermission> ResolvePermissions(UpdateGroupMemberPermissionsRequestDto dto)
    {
        switch (dto.Role)
        {
            case { } requestedRole when !Enum.IsDefined(requestedRole):
                throw new BadRequestException("The specified group role is invalid.");
            case GroupRole.Owner:
                throw new BadRequestException("Ownership can only change through ownership transfer.");
            case { } role when role != GroupRole.Custom && dto.Permissions is null:
                return GroupRolePresets.GetPermissions(role);
            case null or GroupRole.Custom when dto.Permissions is { Count: > 0 }:
            {
                var permissions = dto.Permissions!.ToHashSet();
                if (permissions.Any(permission => !Enum.IsDefined(permission)))
                {
                    throw new BadRequestException("The custom permission set contains an invalid permission.");
                }

                if (!permissions.Contains(GroupPermission.ViewGroup))
                {
                    throw new BadRequestException("An active group member must retain ViewGroup permission.");
                }

                if (permissions.Contains(GroupPermission.DeleteGroup) || permissions.Contains(GroupPermission.TransferOwnership))
                {
                    throw new BadRequestException("DeleteGroup and TransferOwnership are reserved for the group owner.");
                }

                return permissions;
            }
            default:
                throw new BadRequestException("Specify a non-custom role or a non-empty custom permission set.");
        }
    }

    private static void ReplacePermissions(UserGroup membership, IReadOnlySet<GroupPermission> permissions)
    {
        membership.Permissions.Clear();
        foreach (var permission in permissions)
        {
            membership.Permissions.Add(new GroupMemberPermission
            {
                GroupId = membership.GroupId,
                UserId = membership.UserId,
                Permission = permission
            });
        }
    }
}
