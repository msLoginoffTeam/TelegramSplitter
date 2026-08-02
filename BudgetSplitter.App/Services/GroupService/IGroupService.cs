using BudgetSplitter.Common.Dtos.Request;
using BudgetSplitter.Common.Dtos.Response;
using Persistence;

namespace BudgetSplitter.App.Services.GroupService;

public interface IGroupService
{
    Task<IEnumerable<GroupOverviewResponseDto>> GetMyGroupsAsync(Guid userId);
    Task<IEnumerable<GroupOverviewResponseDto>> GetGroupsAsync(long telegramChatId, Guid userId);
    Task<GroupResponseDto> GetGroupAsync(Guid groupId);
    Task<GroupResponseDto> CreateGroupAsync(CreateGroupRequestDto dto, User creator);
    Task UpdateGroupAsync(Guid groupId, UpdateGroupRequestDto dto);
    Task DeleteGroupAsync(Guid groupId);

    Task AddUserAsync(Guid groupId, AddGroupUserRequestDto dto);
    Task RemoveUserAsync(Guid groupId, Guid userId);
    Task UpdateMemberPermissionsAsync(Guid groupId, Guid userId, UpdateGroupMemberPermissionsRequestDto dto);
    Task TransferOwnershipAsync(Guid groupId, Guid newOwnerUserId);
}
