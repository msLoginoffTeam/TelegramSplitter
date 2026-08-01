using BudgetSplitter.Common.Authorization;
using System.ComponentModel.DataAnnotations;

namespace Persistence;

public class GroupMemberPermission
{
    [Required]
    public Guid GroupId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public GroupPermission Permission { get; set; }

    public UserGroup Membership { get; set; } = null!;
}
