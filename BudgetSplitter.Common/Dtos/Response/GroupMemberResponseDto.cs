using BudgetSplitter.Common.Authorization;

namespace BudgetSplitter.Common.Dtos.Response;

public class GroupMemberResponseDto
{
    public Guid UserId { get; set; }
    public long TelegramId { get; set; }
    public string? DisplayName { get; set; }
    public bool IsOwner { get; set; }
    public GroupRole Role { get; set; }
    public IReadOnlyCollection<GroupPermission> Permissions { get; set; } = Array.Empty<GroupPermission>();
}
