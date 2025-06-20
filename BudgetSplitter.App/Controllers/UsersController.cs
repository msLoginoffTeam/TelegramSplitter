using BudgetSplitter.Common.Dtos.Request;
using BudgetSplitter.Common.Dtos.Response;
using Microsoft.AspNetCore.Mvc;

namespace BudgetSplitter.App.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        public UsersController(/*IUserService svc*/) { /*…*/ }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserOverviewResponseDto>>> GetAllUsers()
        {
            throw new NotImplementedException();
        }

        [HttpGet("{userId:guid}")]
        public async Task<ActionResult<UserResponseDto>> GetUser(Guid userId)
        {
            throw new NotImplementedException();
        }
        
        [HttpPost]
        public async Task<ActionResult<Guid>> CreateUser(UserCreateRequestDto userCreateRequestDto)
        {
            throw new NotImplementedException();
        }

        [HttpPut("{userId:guid}")]
        public async Task<IActionResult> UpdateUser(
            Guid userId,
            [FromBody] UpdateUserRequestDto dto)
        {
            throw new NotImplementedException();
        }
    }
}