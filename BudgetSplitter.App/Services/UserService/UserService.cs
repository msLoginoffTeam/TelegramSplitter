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

    public async Task<UserResponseDto> GetUserAsync(Guid userId)
    {
        var u = await _db.Users
                    .Include(user => user.UserGroups)
                    .ThenInclude(userGroup => userGroup.Group).ThenInclude(group => group.UserGroups)
                    .FirstOrDefaultAsync(x => x.Id == userId)
                ?? throw new NotFoundException($"User {userId} not found");

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

    public async Task<UserResponseDto> GetUserByNicknameAsync(string? displayName = null, long? telegramId = null)
    {
        if (telegramId == null && displayName == null)
        {
            throw new BadRequestException("Empty params");
        }

        var user = await _db.Users
                       .Include(user => user.UserGroups)
                       .ThenInclude(userGroup => userGroup.Group)
                       .FirstOrDefaultAsync(x =>
                           (displayName != null && x.DisplayName == displayName) ||
                           telegramId != null && x.TelegramId == telegramId) ??
                   throw new NotFoundException($"User {displayName} not found");

        return new UserResponseDto
        {
            Id = user.Id,
            TelegramId = user.TelegramId,
            DisplayName = user.DisplayName,
            Groups = user.UserGroups.Select(g => new GroupResponseDto
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
            throw new BadRequestException(
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

    public async Task UpdateUserAsync(Guid userId, UpdateUserRequestDto dto)
    {
        var user = await _db.Users.FindAsync(userId)
                   ?? throw new NotFoundException($"User {userId} not found");

        if (!string.IsNullOrWhiteSpace(dto.DisplayName))
            user.DisplayName = dto.DisplayName;

        await _db.SaveChangesAsync();
    }
}