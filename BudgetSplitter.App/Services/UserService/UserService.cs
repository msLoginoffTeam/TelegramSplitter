using BudgetSplitter.Common.Dtos.Response;
using BudgetSplitter.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace BudgetSplitter.App.Services.UserService;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    public UserService(AppDbContext db) => _db = db;

    public async Task<UserResponseDto> GetProfileAsync(Guid userId)
    {
        var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(candidate => candidate.Id == userId)
                ?? throw new NotFoundException($"User {userId} not found");

        return new UserResponseDto
        {
            Id = user.Id,
            TelegramId = user.TelegramId,
            DisplayName = user.DisplayName,
            Username = user.Username
        };
    }
}
