using BudgetSplitter.App.Services.GroupService;
using BudgetSplitter.Common.Dtos.Request;
using BudgetSplitter.Common.Dtos.Response;
using Microsoft.AspNetCore.Mvc;

namespace BudgetSplitter.App.Controllers
{
    /// <summary>
    /// Controller for group CRUD operations and membership management.
    /// </summary>
    [ApiController]
    [Route("api/groups")]
    public class GroupsController : ControllerBase
    {
        private readonly IGroupService _groupService;
        public GroupsController(IGroupService groupService) => _groupService = groupService;

        /// <summary>
        /// Retrieves all groups the specified user belongs to.
        /// </summary>
        /// <param name="userTelegramId">Telegram ID of the user.</param>
        /// <returns>List of GroupOverviewResponseDto for the user’s groups.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GroupOverviewResponseDto>>> GetMyGroups(long userTelegramId)
        {
            var groups = await _groupService.GetMyGroupsAsync(userTelegramId);
            return Ok(groups);
        }

        /// <summary>
        /// Retrieves detailed information about a specific group.
        /// </summary>
        /// <param name="groupId">ID of the group to retrieve.</param>
        /// <returns>GroupResponseDto with full group details.</returns>
        [HttpGet("{groupId:guid}")]
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
            return Ok(await _groupService.CreateGroupAsync(dto));
        }
        
        /// <summary>
        /// Updates an existing group’s details.
        /// </summary>
        /// <param name="groupId">ID of the group to update.</param>
        /// <param name="dto">Updated group data.</param>
        [HttpPut("{groupId:guid}")]
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
        public async Task<IActionResult> AddUser(Guid groupId, [FromBody] AddGroupUserRequestDto dto)
        {
            await _groupService.AddUserAsync(groupId, dto);
            return Ok();
        }

        /// <summary>
        /// Removes a user from the group.
        /// </summary>
        /// <param name="groupId">ID of the group.</param>
        /// <param name="userTelegramId">ID of the user to remove.</param>
        [HttpDelete("{groupId:guid}/users/{userId:guid}")]
        public async Task<IActionResult> RemoveUser(Guid groupId, long userTelegramId)
        {
            await _groupService.RemoveUserAsync(groupId, userTelegramId);
            return Ok();
        }
    }
}