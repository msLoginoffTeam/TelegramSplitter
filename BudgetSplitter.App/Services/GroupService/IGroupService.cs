using BudgetSplitter.Common.Dtos.Request;
using BudgetSplitter.Common.Dtos.Response;

namespace BudgetSplitter.App.Services.GroupService;

public interface IGroupService
{
    Task<IEnumerable<GroupOverviewResponseDto>> GetMyGroupsAsync(long telegramId);
    Task<GroupResponseDto> GetGroupAsync(Guid groupId);
    Task<GroupResponseDto> CreateGroupAsync(CreateGroupRequestDto dto);
    Task UpdateGroupAsync(Guid groupId, UpdateGroupRequestDto dto);
    Task DeleteGroupAsync(Guid groupId);

    Task AddUserAsync(Guid groupId, AddGroupUserRequestDto dto);
    Task RemoveUserAsync(Guid groupId, long userTelegramId);
}