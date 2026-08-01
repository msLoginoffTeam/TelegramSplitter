namespace BudgetSplitter.Common.Authorization;

public static class GroupRolePresets
{
    public static IReadOnlySet<GroupPermission> GetPermissions(GroupRole role) => role switch
    {
        GroupRole.Owner => All,
        GroupRole.Admin => All.Where(permission => permission is not GroupPermission.DeleteGroup and not GroupPermission.TransferOwnership).ToHashSet(),
        GroupRole.Member => new HashSet<GroupPermission>
        {
            GroupPermission.ViewGroup,
            GroupPermission.CreateExpense,
            GroupPermission.UpdateOwnExpense,
            GroupPermission.DeleteOwnExpense,
            GroupPermission.CreatePayment,
            GroupPermission.UpdateOwnPayment,
            GroupPermission.DeleteOwnPayment
        },
        GroupRole.Viewer => new HashSet<GroupPermission> { GroupPermission.ViewGroup },
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Custom has no preset.")
    };

    public static GroupRole ResolveRole(IReadOnlySet<GroupPermission> permissions, bool isOwner)
    {
        if (isOwner) return GroupRole.Owner;

        foreach (var role in new[] { GroupRole.Admin, GroupRole.Member, GroupRole.Viewer })
        {
            if (permissions.SetEquals(GetPermissions(role))) return role;
        }

        return GroupRole.Custom;
    }

    public static readonly IReadOnlySet<GroupPermission> All = Enum.GetValues<GroupPermission>().ToHashSet();
}
