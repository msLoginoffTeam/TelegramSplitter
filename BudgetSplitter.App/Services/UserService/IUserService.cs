using BudgetSplitter.Common.Dtos.Response;

namespace BudgetSplitter.App.Services.UserService;

public interface IUserService
{
    Task<UserResponseDto> GetProfileAsync(Guid userId);
}
