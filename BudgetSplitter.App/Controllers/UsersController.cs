using BudgetSplitter.App.Services.UserService;
using BudgetSplitter.Common.Dtos.Request;
using BudgetSplitter.Common.Dtos.Response;
using Microsoft.AspNetCore.Mvc;

namespace BudgetSplitter.App.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    public UsersController(IUserService userService) => _userService = userService;

    /// <summary>
    /// Получить всех пользователей
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserOverviewResponseDto>>> GetAllUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    /// <summary>
    /// Получить пользователя по Id
    /// </summary>
    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<UserResponseDto>> GetUser(Guid userId)
    {
        var user = await _userService.GetUserAsync(userId);
        return Ok(user);
    }
    
    /// <summary>
    /// Создать нового пользователя
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Guid>> CreateUser([FromBody] UserCreateRequestDto userCreateRequestDto)
    {
        var newId = await _userService.CreateUserAsync(userCreateRequestDto);
        return CreatedAtAction(nameof(GetUser), new { userId = newId }, newId);
    }

    /// <summary>
    /// Обновить данные пользователя
    /// </summary>
    [HttpPut("{userId:guid}")]
    public async Task<IActionResult> UpdateUser(
        Guid userId,
        [FromBody] UpdateUserRequestDto dto)
    {
        await _userService.UpdateUserAsync(userId, dto);
        return NoContent();
    }
}