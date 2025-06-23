using BudgetSplitter.App.Services.UserService;
using BudgetSplitter.Common.Dtos.Request;
using BudgetSplitter.Common.Dtos.Response;
using Microsoft.AspNetCore.Mvc;

namespace BudgetSplitter.App.Controllers;

/// <summary>
/// Controller for user management.
/// </summary>
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    public UsersController(IUserService userService) => _userService = userService;

    /// <summary>
    /// Retrieves an overview of all users in the system.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserOverviewResponseDto>>> GetAllUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    /// <summary>
    /// Retrieves detailed information for a specific user.
    /// </summary>
    /// <param name="userId">ID of the user.</param>
    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<UserResponseDto>> GetUser(Guid userId)
    {
        var user = await _userService.GetUserAsync(userId);
        return Ok(user);
    }

    /// <summary>
    /// Retrieves detailed information for a specific user by nickname or userTelegramId.
    /// </summary>
    /// telegram <param name="nickname"> of the user.</param>
    /// <param name="userTelegramId"></param>
    [HttpGet("find")]
    public async Task<ActionResult<UserResponseDto>> GetUserByNickname(string? nickname = null, long? userTelegramId = null)
    {
        var user = await _userService.GetUserByNicknameAsync(nickname, userTelegramId);
        return Ok(user);
    }
    
    /// <summary>
    /// Creates a new user record.
    /// </summary>
    /// <param name="userCreateRequestDto">User creation data.</param>
    /// <returns>Newly created user’s ID.</returns>
    [HttpPost]
    public async Task<ActionResult<Guid>> CreateUser([FromBody] UserCreateRequestDto userCreateRequestDto)
    {
        var newId = await _userService.CreateUserAsync(userCreateRequestDto);
        return CreatedAtAction(nameof(GetUser), new { userId = newId }, newId);
    }

    /// <summary>
    /// Updates user details.
    /// </summary>
    /// <param name="userId">ID of the user.</param>
    /// <param name="dto">Updated user data.</param>
    [HttpPut("{userId:guid}")]
    public async Task<IActionResult> UpdateUser(
        Guid userId,
        [FromBody] UpdateUserRequestDto dto)
    {
        await _userService.UpdateUserAsync(userId, dto);
        return NoContent();
    }
}