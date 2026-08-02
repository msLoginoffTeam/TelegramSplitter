using BudgetSplitter.App.Services.UserService;
using BudgetSplitter.App.Authorization;
using BudgetSplitter.Common.Dtos.Request;
using BudgetSplitter.Common.Dtos.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetSplitter.App.Controllers;

/// <summary>
/// Controller for user management.
/// </summary>
[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUser;

    public UsersController(IUserService userService, ICurrentUserService currentUser)
    {
        _userService = userService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Retrieves the authenticated user's profile.
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserResponseDto>> GetMe()
    {
        var user = await _currentUser.GetRequiredUserAsync();
        return Ok(await _userService.GetProfileAsync(user.Id));
    }

    /// <summary>
    /// Updates the authenticated user's profile.
    /// </summary>
    /// <param name="dto">Updated user data.</param>
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateUserRequestDto dto)
    {
        var user = await _currentUser.GetRequiredUserAsync();
        await _userService.UpdateUserAsync(user.Id, dto);
        return NoContent();
    }
}
