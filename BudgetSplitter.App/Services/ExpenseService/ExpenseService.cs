using Microsoft.EntityFrameworkCore;
using BudgetSplitter.Common.Dtos;
using BudgetSplitter.Common.Dtos.Request;
using BudgetSplitter.Common.Dtos.Response;
using BudgetSplitter.Common.Exceptions;
using Persistence;

namespace BudgetSplitter.App.Services.ExpenseService;

public class ExpenseService : IExpenseService
{
    private readonly AppDbContext _db;
    public ExpenseService(AppDbContext db) => _db = db;

    public async Task<IEnumerable<ExpenseResponseDto>> GetGroupExpensesAsync(Guid groupId, bool includeDrafts = false)
    {
        var query = _db.Expenses
            .Where(e => e.GroupId == groupId)
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
        if (expense == null) throw new NotFoundException($"Expense {expenseId} not found");

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

        var shares = dto.Shares;
        var sumOthers = shares.Sum(s => s.Amount);
        if (sumOthers > expense.TotalAmount)
            throw new BadRequestException(
                $"Сумма долей ({sumOthers}) превышает общую сумму {expense.TotalAmount}");

        foreach (var s in shares)
        {
            _db.ExpenseShares.Add(new ExpenseShare
            {
                ExpenseId = expense.Id,
                UserId = s.UserId,
                Amount = s.Amount,
                IsPaid = false
            });
        }

        var remainder = expense.TotalAmount - sumOthers;
        _db.ExpenseShares.Add(new ExpenseShare
        {
            ExpenseId = expense.Id,
            UserId = expense.CreatedById,
            Amount = remainder,
            IsPaid = true
        });

        await _db.SaveChangesAsync();

        return await GetExpenseByIdAsync(groupId, expense.Id);
    }

    public async Task UpdateExpenseAsync(Guid expenseId, string title)
    {
        var expense = await _db.Expenses
            .FirstOrDefaultAsync(e => e.Id == expenseId);
        if (expense == null) throw new NotFoundException($"Expense {expenseId} not found");

        expense.Title = title;

        await _db.SaveChangesAsync();
    }

    public async Task UpdateExpenseAsync(Guid expenseId, decimal totalAmount)
    {
        var expense = await _db.Expenses
            .Include(expense => expense.CreatedBy).Include(expense => expense.Shares)
            .ThenInclude(expenseShare => expenseShare.User)
            .FirstOrDefaultAsync(e => e.Id == expenseId);
        if (expense == null) throw new NotFoundException($"Expense {expenseId} not found");

        var value = totalAmount - expense.TotalAmount;

        var creator = expense.Shares.FirstOrDefault(x => x.User == expense.CreatedBy)!;
        creator.Amount += value;

        if (creator.Amount < 0)
        {
            throw new BadRequestException(
                $"Сумма долей превышает новую общую сумму {totalAmount}");
        }

        expense.TotalAmount = totalAmount;

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
        if (expense == null) throw new NotFoundException($"Expense {expenseId} not found in group {groupId}");
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
        ExpenseShareCreateDto share)
    {
        var expense = await _db.Expenses.FindAsync(expenseId)
                      ?? throw new NotFoundException($"Expense {expenseId} not found");
        if (expense.GroupId != groupId)
            throw new BadRequestException("Wrong group");

        var exists = await _db.ExpenseShares
            .AnyAsync(s => s.ExpenseId == expenseId && s.UserId == share.UserId);
        if (exists)
            throw new BadRequestException($"User {share.UserId} is already a participant of expense {expenseId}");

        var sumExisting = await _db.ExpenseShares
            .Where(s => s.ExpenseId == expenseId && s.UserId != expense.CreatedById)
            .SumAsync(s => s.Amount);

        var totalOthers = sumExisting + share.Amount;
        if (totalOthers > expense.TotalAmount)
            throw new BadRequestException(
                $"Сумма долей ({totalOthers}) превышает общую сумму {expense.TotalAmount}");

        _db.ExpenseShares.Add(new ExpenseShare
        {
            ExpenseId = expenseId,
            UserId = share.UserId,
            Amount = share.Amount,
            IsPaid = false
        });

        var remainder = expense.TotalAmount - totalOthers;
        var payerShare = await _db.ExpenseShares
            .FirstOrDefaultAsync(s => s.ExpenseId == expenseId && s.UserId == expense.CreatedById);

        if (payerShare == null)
        {
            _db.ExpenseShares.Add(new ExpenseShare
            {
                ExpenseId = expenseId,
                UserId = expense.CreatedById,
                Amount = remainder,
                IsPaid = true
            });
        }
        else
        {
            payerShare.Amount = remainder;
        }

        await _db.SaveChangesAsync();
    }

    public async Task UpdateExpenseParticipantAsync(Guid groupId, Guid expenseId,
        ExpenseShareCreateDto shareDto)
    {
        var share = await _db.ExpenseShares
                        .Include(x => x.Expense)
                        .FirstOrDefaultAsync(x => x.ExpenseId == expenseId && x.UserId == shareDto.UserId)
                    ?? throw new NotFoundException($"Share for user {shareDto.UserId} not found");

        if (share.Expense.GroupId != groupId)
            throw new BadRequestException("Wrong group");

        var expense = share.Expense;
        var sumOthers = await _db.ExpenseShares
            .Where(x => x.ExpenseId == expenseId && x.UserId != expense.CreatedById)
            .SumAsync(x => x.Amount);

        sumOthers = sumOthers - share.Amount + shareDto.Amount;
        share.Amount = shareDto.Amount;

        if (sumOthers > expense.TotalAmount)
            throw new BadRequestException(
                $"Сумма долей ({sumOthers}) превышает общую сумму {expense.TotalAmount}");

        var remainder = expense.TotalAmount - sumOthers;
        var payerShare = await _db.ExpenseShares
            .FirstOrDefaultAsync(x => x.ExpenseId == expenseId && x.UserId == expense.CreatedById);

        if (payerShare == null)
        {
            _db.ExpenseShares.Add(new ExpenseShare
            {
                ExpenseId = expenseId,
                UserId = expense.CreatedById,
                Amount = remainder,
                IsPaid = true
            });
        }
        else
        {
            payerShare.Amount = remainder;
        }


        await _db.SaveChangesAsync();
    }

    public async Task RemoveExpenseParticipantAsync(Guid groupId, Guid expenseId, Guid userId)
    {
        var share = await _db.ExpenseShares
            .Include(x => x.Expense)
            .FirstOrDefaultAsync(x => x.ExpenseId == expenseId && x.UserId == userId);

        if (share == null || share.Expense.GroupId != groupId)
            return;

        _db.ExpenseShares.Remove(share);
        await _db.SaveChangesAsync();

        var expense = share.Expense;
        var sumOthers = await _db.ExpenseShares
            .Where(x => x.ExpenseId == expenseId && x.UserId != expense.CreatedById)
            .SumAsync(x => x.Amount);

        var remainder = expense.TotalAmount - sumOthers;
        var payerShare = await _db.ExpenseShares
            .FirstOrDefaultAsync(x => x.ExpenseId == expenseId && x.UserId == expense.CreatedById);

        if (payerShare == null)
        {
            _db.ExpenseShares.Add(new ExpenseShare
            {
                ExpenseId = expenseId,
                UserId = expense.CreatedById,
                Amount = remainder,
                IsPaid = true
            });
        }
        else
        {
            payerShare.Amount = remainder;
        }

        await _db.SaveChangesAsync();
    }
}