using BudgetSplitter.Common.Dtos.Request;
using BudgetSplitter.Common.Dtos.Response;

namespace BudgetSplitter.App.Services.UserService
{
    public interface IUserService
    {
        Task<IEnumerable<UserOverviewResponseDto>> GetAllUsersAsync();
        Task<UserResponseDto> GetUserAsync(Guid userId);
        Task<UserResponseDto> GetUserByNicknameAsync(string? displayName = null, long? telegramId = null);
        Task<Guid> CreateUserAsync(UserCreateRequestDto dto);
        Task UpdateUserAsync(Guid userId, UpdateUserRequestDto dto);
    }
}