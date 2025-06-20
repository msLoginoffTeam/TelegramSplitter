using BudgetSplitter.Common.Dtos.Request;
using BudgetSplitter.Common.Dtos.Response;
using Microsoft.AspNetCore.Mvc;

namespace BudgetSplitter.App.Controllers
{
    [ApiController]
    [Route("api/groups")]
    public class GroupsController : ControllerBase
    {
        public GroupsController(/*IGroupService svc*/) { /*…*/ }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<GroupOverviewResponseDto>>> GetMyGroups()
        {
            throw new NotImplementedException();
        }

        [HttpGet("{groupId:guid}")]
        public async Task<ActionResult<GroupResponseDto>> GetGroup(Guid groupId)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        public async Task<ActionResult<GroupResponseDto>> CreateGroup([FromBody] CreateGroupRequestDto dto)
        {
            throw new NotImplementedException();
        }

        [HttpPut("{groupId:guid}")]
        public async Task<IActionResult> UpdateGroup(
            Guid groupId,
            [FromBody] UpdateGroupRequestDto dto)
        {
            throw new NotImplementedException();
        }

        [HttpDelete("{groupId:guid}")]
        public async Task<IActionResult> DeleteGroup(Guid groupId)
        {
            throw new NotImplementedException();
        }

        [HttpPost("{groupId:guid}/users")]
        public async Task<IActionResult> AddUser(Guid groupId, [FromBody] AddGroupUserRequestDto dto)
        {
            throw new NotImplementedException();
        }

        [HttpDelete("{groupId:guid}/users/{userId:guid}")]
        public async Task<IActionResult> RemoveUser(Guid groupId, Guid userId)
        {
            throw new NotImplementedException();
        }
    }
}