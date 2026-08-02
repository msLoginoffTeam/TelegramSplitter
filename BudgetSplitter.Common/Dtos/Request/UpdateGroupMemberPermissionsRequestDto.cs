using BudgetSplitter.Common.Authorization;

namespace BudgetSplitter.Common.Dtos.Request;

public class UpdateGroupMemberPermissionsRequestDto
{
    public GroupRole? Role { get; set; }
    public IReadOnlyCollection<GroupPermission>? Permissions { get; set; }
}
