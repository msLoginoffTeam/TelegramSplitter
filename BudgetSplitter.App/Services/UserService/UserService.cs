using BudgetSplitter.Common.Dtos.Request;
using BudgetSplitter.Common.Dtos.Response;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace BudgetSplitter.App.Services.UserService;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    public UserService(AppDbContext db) => _db = db;

    public async Task<IEnumerable<UserOverviewResponseDto>> GetAllUsersAsync()
    {
        var users = await _db.Users
            .AsNoTracking()
            .ToListAsync();

        return users.Select(u => new UserOverviewResponseDto
        {
            Id = u.Id,
            TelegramId = u.TelegramId,
            DisplayName = u.DisplayName
        });
    }

    public async Task<UserResponseDto> GetUserAsync(long userTelegramId)
    {
        var u = await _db.Users
                    .Include(user => user.UserGroups)
                    .ThenInclude(userGroup => userGroup.Group).ThenInclude(group => group.UserGroups)
                    .FirstOrDefaultAsync(x => x.TelegramId == userTelegramId)
                ?? throw new KeyNotFoundException($"User {userTelegramId} not found");

        return new UserResponseDto
        {
            Id = u.Id,
            TelegramId = u.TelegramId,
            DisplayName = u.DisplayName,
            Groups = u.UserGroups.Select(g => new GroupResponseDto
            {
                Id = g.GroupId,
                TelegramChatId = g.Group.TelegramChatId,
                Title = g.Group.Title
            })
        };
    }

    public async Task<Guid> CreateUserAsync(UserCreateRequestDto dto)
    {
        var exists = await _db.Users
            .AnyAsync(u => u.TelegramId == dto.TelegramId);
        if (exists)
            throw new InvalidOperationException(
                $"User with TelegramId {dto.TelegramId} already exists");

        var user = new User
        {
            TelegramId = dto.TelegramId,
            DisplayName = dto.DisplayName
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user.Id;
    }

    public async Task UpdateUserAsync(long userTelegramId, UpdateUserRequestDto dto)
    {
        var user = _db.Users.FirstOrDefault(x => x.TelegramId == userTelegramId)
                   ?? throw new KeyNotFoundException($"User {userTelegramId} not found");

        if (!string.IsNullOrWhiteSpace(dto.DisplayName))
            user.DisplayName = dto.DisplayName;

        await _db.SaveChangesAsync();
    }
}