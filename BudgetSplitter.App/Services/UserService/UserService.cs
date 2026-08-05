using BudgetSplitter.Common.Dtos.Request;
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

    public async Task UpdateUserAsync(Guid userId, UpdateUserRequestDto dto)
    {
        var user = await _db.Users.FindAsync(userId)
                   ?? throw new NotFoundException($"User {userId} not found");

        if (!string.IsNullOrWhiteSpace(dto.DisplayName))
            user.DisplayName = dto.DisplayName;

        await _db.SaveChangesAsync();
    }
}
