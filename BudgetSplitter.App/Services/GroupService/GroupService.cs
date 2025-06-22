using BudgetSplitter.Common.Dtos.Request;
using BudgetSplitter.Common.Dtos.Response;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace BudgetSplitter.App.Services.GroupService;

public class GroupService : IGroupService
{
    private readonly AppDbContext _db;
    public GroupService(AppDbContext db) => _db = db;

    public async Task<IEnumerable<GroupOverviewResponseDto>> GetMyGroupsAsync(long telegramId)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.TelegramId == telegramId);

        if (user == null)
        {
            user = new User { TelegramId = telegramId };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        var groups = await _db.Groups
            .Where(g => g.CreatedBy.Id == user.Id || g.UserGroups.Any(ug => ug.UserId == user.Id))
            .AsNoTracking()
            .ToListAsync();

        return groups
            .Select(g => new GroupOverviewResponseDto
            {
                Id = g.Id,
                Title = g.Title
            });
    }

    public async Task<GroupResponseDto> GetGroupAsync(Guid groupId)
    {
        var group = await _db.Groups
            .Include(g => g.UserGroups)
            .ThenInclude(ug => ug.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
            throw new KeyNotFoundException($"Group {groupId} not found");

        return new GroupResponseDto
        {
            Id = group.Id,
            Title = group.Title,
            TelegramChatId = group.TelegramChatId,
            Users = group.UserGroups
                .Select(ug => new UserResponseDto
                {
                    Id = ug.User.Id,
                    TelegramId = ug.User.TelegramId,
                    DisplayName = ug.User.DisplayName
                })
                .ToList()
        };
    }

    public async Task<GroupResponseDto> CreateGroupAsync(CreateGroupRequestDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.TelegramId == dto.CreatedByTelegramId);

        if (user == null)
        {
            user = new User { TelegramId = dto.CreatedByTelegramId };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }
        
        var group = new Group
        {
            Title = dto.Title,
            TelegramChatId = dto.TelegramChatId,
            CreatedBy = user
        };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        // добавляем создателя как участника, если передан TelegramChatId
        // (либо этот шаг можно делать отдельным вызовом AddUserAsync)
        return await GetGroupAsync(group.Id);
    }

    public async Task UpdateGroupAsync(Guid groupId, UpdateGroupRequestDto dto)
    {
        var group = await _db.Groups.FindAsync(groupId);
        if (group == null)
            throw new KeyNotFoundException($"Group {groupId} not found");

        group.Title = dto.Title;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteGroupAsync(Guid groupId)
    {
        // soft delete? пока — жёсткое
        var group = await _db.Groups.FindAsync(groupId);
        if (group == null) return;
        _db.Groups.Remove(group);
        await _db.SaveChangesAsync();
    }

    public async Task AddUserAsync(Guid groupId, AddGroupUserRequestDto dto)
    {
        var group = await _db.Groups.FindAsync(groupId)
                    ?? throw new KeyNotFoundException($"Group {groupId} not found");

        // находим или создаём пользователя
        var user = await _db.Users.FirstOrDefaultAsync(u => u.TelegramId == dto.TelegramId);
        if (user == null)
        {
            user = new User { TelegramId = dto.TelegramId };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        if (!await _db.UserGroups.AnyAsync(ug => ug.GroupId == group.Id && ug.UserId == user.Id))
        {
            _db.UserGroups.Add(new UserGroup
            {
                Group = group,
                User = user
            });
        }
        else
        {
            throw new ArgumentException($"User with id {dto.TelegramId} already exists in group {groupId}");
        }

        await _db.SaveChangesAsync();
    }

    public async Task RemoveUserAsync(Guid groupId, long userTelegramId)
    {
        var ug = await _db.UserGroups
            .FirstOrDefaultAsync(x => x.GroupId == groupId && x.User.TelegramId == userTelegramId);
        if (ug != null)
        {
            _db.UserGroups.Remove(ug);
            await _db.SaveChangesAsync();
        }
        else
        {
            throw new ArgumentException($"User with id {userTelegramId} does not exist in group {groupId}");
        }
    }
}