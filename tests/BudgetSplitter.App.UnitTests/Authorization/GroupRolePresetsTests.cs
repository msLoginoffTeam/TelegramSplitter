using BudgetSplitter.Common.Authorization;

namespace BudgetSplitter.App.UnitTests.Authorization;

public sealed class GroupRolePresetsTests
{
    [Fact]
    public void AdminPreset_ContainsEveryPermissionExceptOwnerOnlyPermissions()
    {
        var permissions = GroupRolePresets.GetPermissions(GroupRole.Admin);

        Assert.DoesNotContain(GroupPermission.DeleteGroup, permissions);
        Assert.DoesNotContain(GroupPermission.TransferOwnership, permissions);
        Assert.Contains(GroupPermission.ManagePermissions, permissions);
        Assert.Contains(GroupPermission.DeleteAnyPayment, permissions);
    }

    [Fact]
    public void ResolveRole_WithExactMemberPreset_ReturnsMember()
    {
        var role = GroupRolePresets.ResolveRole(GroupRolePresets.GetPermissions(GroupRole.Member), isOwner: false);

        Assert.Equal(GroupRole.Member, role);
    }

    [Fact]
    public void ResolveRole_WithCustomPermissions_ReturnsCustom()
    {
        var role = GroupRolePresets.ResolveRole(
            new HashSet<GroupPermission>
            {
                GroupPermission.ViewGroup,
                GroupPermission.CreatePayment
            },
            isOwner: false);

        Assert.Equal(GroupRole.Custom, role);
    }

    [Fact]
    public void ResolveRole_ForOwner_ReturnsOwnerRegardlessOfPermissionSet()
    {
        var role = GroupRolePresets.ResolveRole(
            new HashSet<GroupPermission> { GroupPermission.ViewGroup },
            isOwner: true);

        Assert.Equal(GroupRole.Owner, role);
    }
}
