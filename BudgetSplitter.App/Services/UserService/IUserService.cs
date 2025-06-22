using BudgetSplitter.Common.Dtos.Request;
using BudgetSplitter.Common.Dtos.Response;

namespace BudgetSplitter.App.Services.UserService
{
    public interface IUserService
    {
        Task<IEnumerable<UserOverviewResponseDto>> GetAllUsersAsync();
        Task<UserResponseDto> GetUserAsync(long userTelegramId);
        Task<Guid> CreateUserAsync(UserCreateRequestDto dto);
        Task UpdateUserAsync(long userTelegramId, UpdateUserRequestDto dto);
    }
}