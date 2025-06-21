using BudgetSplitter.App.Services.GroupService;
using BudgetSplitter.Common.Dtos.Request;
using BudgetSplitter.Common.Dtos.Response;
using Microsoft.AspNetCore.Mvc;

namespace BudgetSplitter.App.Controllers
{
    [ApiController]
    [Route("api/groups")]
    public class GroupsController : ControllerBase
    {
        private readonly IGroupService _groupService;
        public GroupsController(IGroupService groupService) => _groupService = groupService;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<GroupOverviewResponseDto>>> GetMyGroups(long userTelegramId)
        {
            var groups = await _groupService.GetMyGroupsAsync(userTelegramId);
            return Ok(groups);
        }

        [HttpGet("{groupId:guid}")]
        public async Task<ActionResult<GroupResponseDto>> GetGroup(Guid groupId)
        {
            var group = await _groupService.GetGroupAsync(groupId);
            return Ok(group);
        }

        [HttpPost]
        public async Task<ActionResult<GroupResponseDto>> CreateGroup([FromBody] CreateGroupRequestDto dto)
        {
            return Ok(await _groupService.CreateGroupAsync(dto));
        }

        [HttpPut("{groupId:guid}")]
        public async Task<IActionResult> UpdateGroup(
            Guid groupId,
            [FromBody] UpdateGroupRequestDto dto)
        {
            await _groupService.UpdateGroupAsync(groupId, dto);
            return Ok();
        }

        [HttpDelete("{groupId:guid}")]
        public async Task<IActionResult> DeleteGroup(Guid groupId)
        {
            await _groupService.DeleteGroupAsync(groupId);
            return Ok();
        }

        [HttpPost("{groupId:guid}/users")]
        public async Task<IActionResult> AddUser(Guid groupId, [FromBody] AddGroupUserRequestDto dto)
        {
            await _groupService.AddUserAsync(groupId, dto);
            return Ok();
        }

        [HttpDelete("{groupId:guid}/users/{userId:guid}")]
        public async Task<IActionResult> RemoveUser(Guid groupId, Guid userId)
        {
            await _groupService.RemoveUserAsync(groupId, userId);
            return Ok();
        }
    }
}