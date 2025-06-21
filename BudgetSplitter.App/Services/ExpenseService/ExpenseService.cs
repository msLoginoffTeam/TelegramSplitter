using Microsoft.EntityFrameworkCore;
using BudgetSplitter.Common.Dtos;
using BudgetSplitter.Common.Dtos.Request;
using BudgetSplitter.Common.Dtos.Response;
using Persistence;

namespace BudgetSplitter.App.Services.ExpenseService;

public class ExpenseService : IExpenseService
{
    private readonly AppDbContext _db;
    public ExpenseService(AppDbContext db) => _db = db;

    public async Task<IEnumerable<ExpenseResponseDto>> GetGroupExpensesAsync(Guid groupId, bool includeDrafts = false)
    {
        var query = _db.Expenses
            .Where(e => e.GroupId == groupId && (includeDrafts || !e.IsDraft))
            .AsNoTracking();

        return query.Select(e => new ExpenseResponseDto
        {
            Id = e.Id,
            Title = e.Title,
            TotalAmount = e.TotalAmount,
            CreatedById = e.CreatedById,
            CreatedAt = e.CreatedAt,
            IsDraft = e.IsDraft,
            Shares = e.Shares.Select(s => new ExpenseShareResponseDto
            {
                UserId = s.UserId,
                Amount = s.Amount,
                IsPaid = s.IsPaid
            }).ToList()
        });
    }

    public async Task<ExpenseResponseDto> GetExpenseByIdAsync(Guid groupId, Guid expenseId)
    {
        var expense = await _db.Expenses
            .Where(x => x.GroupId == groupId && x.Id == expenseId)
            .Include(x => x.Shares)
            .AsNoTracking()
            .FirstOrDefaultAsync();
        if (expense == null) throw new KeyNotFoundException($"Expense {expenseId} not found in group {groupId}");

        return new ExpenseResponseDto
        {
            Id = expense.Id,
            Title = expense.Title,
            TotalAmount = expense.TotalAmount,
            CreatedById = expense.CreatedById,
            CreatedAt = expense.CreatedAt,
            IsDraft = expense.IsDraft,
            Shares = expense.Shares.Select(s => new ExpenseShareResponseDto
            {
                UserId = s.UserId,
                Amount = s.Amount,
                IsPaid = s.IsPaid
            }).ToList()
        };
    }

    public async Task<ExpenseResponseDto> CreateExpenseAsync(Guid groupId, CreateExpenseRequestDto dto)
    {
        var expense = new Expense
        {
            GroupId = groupId,
            Title = dto.Title,
            TotalAmount = dto.TotalAmount,
            CreatedById = dto.CreatedById,
            CreatedAt = DateTime.UtcNow,
            IsDraft = dto.IsDraft
        };
        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();

        foreach (var share in dto.Shares)
        {
            _db.ExpenseShares.Add(new ExpenseShare
            {
                Expense = expense,
                UserId = share.UserId,
                Amount = share.Amount,
                IsPaid = false
            });
        }

        await _db.SaveChangesAsync();

        return await GetExpenseByIdAsync(groupId, expense.Id);
    }

    public async Task UpdateExpenseAsync(Guid groupId, Guid expenseId, UpdateExpenseRequestDto dto)
    {
        var expense = await _db.Expenses
            .FirstOrDefaultAsync(e => e.GroupId == groupId && e.Id == expenseId);
        if (expense == null) throw new KeyNotFoundException($"Expense {expenseId} not found in group {groupId}");

        expense.Title = dto.Title ?? expense.Title;
        expense.TotalAmount = dto.TotalAmount ?? expense.TotalAmount;
        if (dto.IsDraft.HasValue) expense.IsDraft = dto.IsDraft.Value;

        await _db.SaveChangesAsync();
    }

    public async Task DeleteExpenseAsync(Guid groupId, Guid expenseId)
    {
        var expense = await _db.Expenses
            .FirstOrDefaultAsync(e => e.GroupId == groupId && e.Id == expenseId);
        if (expense == null) return;
        _db.Expenses.Remove(expense);
        await _db.SaveChangesAsync();
    }

    public async Task ConfirmExpenseAsync(Guid groupId, Guid expenseId)
    {
        var expense = await _db.Expenses
            .FirstOrDefaultAsync(e => e.GroupId == groupId && e.Id == expenseId);
        if (expense == null) throw new KeyNotFoundException($"Expense {expenseId} not found in group {groupId}");
        expense.IsDraft = false;
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<ExpenseShareResponseDto>> GetExpenseParticipantsAsync(Guid groupId, Guid expenseId)
    {
        var shares = await _db.ExpenseShares
            .Where(s => s.ExpenseId == expenseId && s.Expense.GroupId == groupId)
            .AsNoTracking()
            .ToListAsync();
        return shares.Select(s => new ExpenseShareResponseDto
        {
            UserId = s.UserId,
            Amount = s.Amount,
            IsPaid = s.IsPaid
        });
    }

    public async Task AddExpenseParticipantsAsync(Guid groupId, Guid expenseId,
        IEnumerable<ExpenseShareCreateDto> shares)
    {
        var exists = await _db.Expenses
            .AnyAsync(e => e.GroupId == groupId && e.Id == expenseId);
        if (!exists) throw new KeyNotFoundException($"Expense {expenseId} not found in group {groupId}");

        foreach (var share in shares)
        {
            var existing = await _db.ExpenseShares
                .FirstOrDefaultAsync(s => s.ExpenseId == expenseId && s.UserId == share.UserId);

            if (existing is null)
            {
                _db.ExpenseShares.Add(new ExpenseShare {
                    ExpenseId  = expenseId,
                    UserId     = share.UserId,
                    Amount     = share.Amount,
                    IsPaid     = false
                });
            }
            else
            {
                existing.Amount = share.Amount;
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task UpdateExpenseParticipantAsync(Guid groupId, Guid expenseId,
        ExpenseShareCreateDto shareDto)
    {
        var share = await _db.ExpenseShares
            .Include(s => s.Expense)
            .FirstOrDefaultAsync(s => s.ExpenseId == expenseId && s.UserId == shareDto.UserId && s.Expense.GroupId == groupId);
        if (share == null) throw new KeyNotFoundException($"Share for user {shareDto.UserId} not found in expense {expenseId}");
        share.Amount = shareDto.Amount;
        await _db.SaveChangesAsync();
    }

    public async Task RemoveExpenseParticipantAsync(Guid groupId, Guid expenseId, Guid userId)
    {
        var share = await _db.ExpenseShares
            .Include(s => s.Expense)
            .FirstOrDefaultAsync(s => s.ExpenseId == expenseId && s.UserId == userId && s.Expense.GroupId == groupId);
        if (share != null)
        {
            _db.ExpenseShares.Remove(share);
            await _db.SaveChangesAsync();
        }
    }
}