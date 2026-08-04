using BudgetSplitter.App.Authorization;
using BudgetSplitter.App.Services.GroupService;
using BudgetSplitter.Common.Authorization;
using BudgetSplitter.Common.Dtos.Request;
using BudgetSplitter.Common.Dtos.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetSplitter.App.Controllers
{
    /// <summary>
    /// Controller for group CRUD operations and membership management.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/groups")]
    public class GroupsController : ControllerBase
    {
        private readonly IGroupService _groupService;
        private readonly ICurrentUserService _currentUser;
        public GroupsController(IGroupService groupService, ICurrentUserService currentUser)
        {
            _groupService = groupService;
            _currentUser = currentUser;
        }

        /// <summary>
        /// Retrieves groups the authenticated user belongs to.
        /// </summary>
        /// <returns>List of GroupOverviewResponseDto for the user’s groups.</returns>
        [HttpGet("my")]
        public async Task<ActionResult<IEnumerable<GroupOverviewResponseDto>>> GetMyGroups()
        {
            var user = await _currentUser.GetRequiredUserAsync();
            var groups = await _groupService.GetMyGroupsAsync(user.Id);
            return Ok(groups);
        }
        
        /// <summary>
        /// Retrieves all groups the specified chat belongs to.
        /// </summary>
        /// <returns>List of GroupOverviewResponseDto for the user’s groups.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GroupOverviewResponseDto>>> GetGroups(long telegramChatId)
        {
            var user = await _currentUser.GetRequiredUserAsync();
            var groups = await _groupService.GetGroupsAsync(telegramChatId, user.Id);
            return Ok(groups);
        }

        /// <summary>
        /// Retrieves detailed information about a specific group.
        /// </summary>
        /// <param name="groupId">ID of the group to retrieve.</param>
        /// <returns>GroupResponseDto with full group details.</returns>
        [HttpGet("{groupId:guid}")]
        [RequireGroupPermission(GroupPermission.ViewGroup)]
        public async Task<ActionResult<GroupResponseDto>> GetGroup(Guid groupId)
        {
            var group = await _groupService.GetGroupAsync(groupId);
            return Ok(group);
        }

        /// <summary>
        /// Creates a new group.
        /// </summary>
        /// <param name="dto">Data for creating the group.</param>
        /// <returns>The created GroupResponseDto.</returns>
        [HttpPost]
        public async Task<ActionResult<GroupResponseDto>> CreateGroup([FromBody] CreateGroupRequestDto dto)
        {
            var user = await _currentUser.GetRequiredUserAsync();
            var group = await _groupService.CreateGroupAsync(dto, user);
            return CreatedAtAction(nameof(GetGroup), new { groupId = group.Id }, group);
        }
        
        /// <summary>
        /// Updates an existing group’s details.
        /// </summary>
        /// <param name="groupId">ID of the group to update.</param>
        /// <param name="dto">Updated group data.</param>
        [HttpPut("{groupId:guid}")]
        [RequireGroupPermission(GroupPermission.UpdateGroup)]
        public async Task<IActionResult> UpdateGroup(
            Guid groupId,
            [FromBody] UpdateGroupRequestDto dto)
        {
            await _groupService.UpdateGroupAsync(groupId, dto);
            return Ok();
        }

        /// <summary>
        /// Deletes a group.
        /// </summary>
        /// <param name="groupId">ID of the group to delete.</param>
        [HttpDelete("{groupId:guid}")]
        [RequireGroupPermission(GroupPermission.DeleteGroup)]
        public async Task<IActionResult> DeleteGroup(Guid groupId)
        {
            await _groupService.DeleteGroupAsync(groupId);
            return Ok();
        }

        /// <summary>
        /// Adds a user to the group.
        /// </summary>
        /// <param name="groupId">ID of the group.</param>
        /// <param name="dto">Details of the user to add.</param>
        [HttpPost("{groupId:guid}/users")]
        [RequireGroupPermission(GroupPermission.ManageMembers)]
        public async Task<IActionResult> AddUser(Guid groupId, [FromBody] AddGroupUserRequestDto dto)
        {
            await _groupService.AddUserAsync(groupId, dto);
            return Ok();
        }

        /// <summary>
        /// Removes a user from the group.
        /// </summary>
        /// <param name="groupId">ID of the group.</param>
        /// <param name="userId">ID of the user to remove.</param>
        [HttpDelete("{groupId:guid}/users/{userId:guid}")]
        [RequireGroupPermission(GroupPermission.ManageMembers)]
        public async Task<IActionResult> RemoveUser(Guid groupId, Guid userId)
        {
            await _groupService.RemoveUserAsync(groupId, userId);
            return Ok();
        }

        [HttpPut("{groupId:guid}/users/{userId:guid}/permissions")]
        [RequireGroupPermission(GroupPermission.ManagePermissions)]
        public async Task<IActionResult> UpdateMemberPermissions(
            Guid groupId,
            Guid userId,
            [FromBody] UpdateGroupMemberPermissionsRequestDto dto)
        {
            await _groupService.UpdateMemberPermissionsAsync(groupId, userId, dto);
            return NoContent();
        }

        [HttpPost("{groupId:guid}/ownership")]
        [RequireGroupPermission(GroupPermission.TransferOwnership)]
        public async Task<IActionResult> TransferOwnership(
            Guid groupId,
            [FromBody] TransferGroupOwnershipRequestDto dto)
        {
            await _groupService.TransferOwnershipAsync(groupId, dto.NewOwnerUserId);
            return NoContent();
        }
    }
}
