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
            PayerId = e.PayerId,
            PayerName = e.Payer.DisplayName,
            CreatedByUserId = e.CreatedByUserId,
            CreatedAt = e.CreatedAt,
            IsDraft = e.IsDraft,
            Shares = e.Shares.Select(s => new ExpenseShareResponseDto
            {
                UserId = s.UserId,
                UserName = s.User.DisplayName,
                Amount = s.Amount,
                IsPaid = s.IsPaid
            }).ToList()
        });
    }

    public async Task<ExpenseResponseDto> GetExpenseByIdAsync(Guid groupId, Guid expenseId)
    {
        var expense = await _db.Expenses
            .Where(x => x.GroupId == groupId && x.Id == expenseId)
            .Include(x => x.Shares).ThenInclude(expenseShare => expenseShare.User).Include(expense => expense.Payer)
            .AsNoTracking()
            .FirstOrDefaultAsync();
        if (expense == null) throw new NotFoundException($"Expense {expenseId} not found");

        return new ExpenseResponseDto
        {
            Id = expense.Id,
            Title = expense.Title,
            TotalAmount = expense.TotalAmount,
            PayerId = expense.PayerId,
            PayerName = expense.Payer.DisplayName,
            CreatedByUserId = expense.CreatedByUserId,
            CreatedAt = expense.CreatedAt,
            IsDraft = expense.IsDraft,
            Shares = expense.Shares.Select(s => new ExpenseShareResponseDto
            {
                UserId = s.UserId,
                UserName = s.User.DisplayName,
                Amount = s.Amount,
                IsPaid = s.IsPaid
            }).ToList()
        };
    }

    public async Task<ExpenseResponseDto> CreateExpenseAsync(Guid groupId, CreateExpenseRequestDto dto,
        Guid createdByUserId)
    {
        EnsureValidTitle(dto.Title);
        EnsurePositiveAmount(dto.TotalAmount, "Total amount");

        var shareUserIds = dto.Shares.Select(share => share.UserId).ToArray();
        if (shareUserIds.Contains(dto.PayerId))
        {
            throw new BadRequestException("Payer must not be duplicated in expense shares.");
        }

        if (shareUserIds.Distinct().Count() != shareUserIds.Length)
        {
            throw new BadRequestException("An expense can contain only one share per participant.");
        }

        if (dto.Shares.Any(share => share.Amount <= 0))
        {
            throw new BadRequestException("Each expense participant share must be positive.");
        }

        await EnsureGroupMembersAsync(groupId, shareUserIds.Append(dto.PayerId));

        var sharesTotal = dto.Shares.Sum(share => share.Amount);
        if (sharesTotal > dto.TotalAmount)
        {
            throw new BadRequestException(
                $"Сумма долей ({sharesTotal}) превышает общую сумму {dto.TotalAmount}");
        }

        var expense = new Expense
        {
            GroupId = groupId,
            Title = dto.Title,
            TotalAmount = dto.TotalAmount,
            PayerId = dto.PayerId,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            IsDraft = dto.IsDraft
        };
        _db.Expenses.Add(expense);

        foreach (var s in dto.Shares)
        {
            _db.ExpenseShares.Add(new ExpenseShare
            {
                ExpenseId = expense.Id,
                UserId = s.UserId,
                Amount = s.Amount,
                IsPaid = false
            });
        }

        var remainder = expense.TotalAmount - sharesTotal;
        _db.ExpenseShares.Add(new ExpenseShare
        {
            ExpenseId = expense.Id,
            UserId = expense.PayerId,
            Amount = remainder,
            IsPaid = true
        });

        await _db.SaveChangesAsync();

        return await GetExpenseByIdAsync(groupId, expense.Id);
    }

    public async Task UpdateExpenseAsync(Guid expenseId, string title)
    {
        EnsureValidTitle(title);

        var expense = await _db.Expenses
            .FirstOrDefaultAsync(e => e.Id == expenseId);
        if (expense == null) throw new NotFoundException($"Expense {expenseId} not found");

        expense.Title = title;

        await _db.SaveChangesAsync();
    }

    public async Task UpdateExpenseAsync(Guid expenseId, decimal totalAmount)
    {
        EnsurePositiveAmount(totalAmount, "Total amount");

        var expense = await _db.Expenses
            .Include(expense => expense.Payer).Include(expense => expense.Shares)
            .ThenInclude(expenseShare => expenseShare.User)
            .FirstOrDefaultAsync(e => e.Id == expenseId);
        if (expense == null) throw new NotFoundException($"Expense {expenseId} not found");

        var value = totalAmount - expense.TotalAmount;

        var payer = expense.Shares.FirstOrDefault(share => share.UserId == expense.PayerId)
                    ?? throw new BadRequestException("Payer share is missing from the expense.");
        payer.Amount += value;

        if (payer.Amount < 0)
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
            .FirstOrDefaultAsync(e => e.Id == expenseId && e.GroupId == groupId);
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
            .AsNoTracking().Include(expenseShare => expenseShare.User)
            .ToListAsync();
        return shares.Select(s => new ExpenseShareResponseDto
        {
            UserId = s.UserId,
            UserName = s.User.DisplayName,
            Amount = s.Amount,
            IsPaid = s.IsPaid
        });
    }

    public async Task AddExpenseParticipantsAsync(Guid groupId, Guid expenseId,
        ExpenseShareCreateDto share)
    {
        EnsurePositiveAmount(share.Amount, "Expense participant share");

        var expense = await _db.Expenses.FindAsync(expenseId)
                      ?? throw new NotFoundException($"Expense {expenseId} not found");
        if (expense.GroupId != groupId)
            throw new BadRequestException("Wrong group");

        await EnsureGroupMembersAsync(groupId, [share.UserId]);

        var exists = await _db.ExpenseShares
            .AnyAsync(s => s.ExpenseId == expenseId && s.UserId == share.UserId);
        if (exists)
            throw new BadRequestException($"User {share.UserId} is already a participant of expense {expenseId}");

        var sumExisting = await _db.ExpenseShares
            .Where(s => s.ExpenseId == expenseId && s.UserId != expense.PayerId)
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
            .FirstOrDefaultAsync(s => s.ExpenseId == expenseId && s.UserId == expense.PayerId);

        if (payerShare == null)
        {
            _db.ExpenseShares.Add(new ExpenseShare
            {
                ExpenseId = expenseId,
                UserId = expense.PayerId,
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
        EnsurePositiveAmount(shareDto.Amount, "Expense participant share");

        var share = await _db.ExpenseShares
                        .Include(x => x.Expense)
                        .FirstOrDefaultAsync(x => x.ExpenseId == expenseId && x.UserId == shareDto.UserId)
                    ?? throw new NotFoundException($"Share for user {shareDto.UserId} not found");

        if (share.Expense.GroupId != groupId)
            throw new BadRequestException("Wrong group");

        var expense = share.Expense;
        if (share.UserId == expense.PayerId)
        {
            throw new BadRequestException("Payer share is calculated automatically and cannot be updated directly.");
        }

        var sumOthers = await _db.ExpenseShares
            .Where(x => x.ExpenseId == expenseId && x.UserId != expense.PayerId)
            .SumAsync(x => x.Amount);

        sumOthers = sumOthers - share.Amount + shareDto.Amount;
        share.Amount = shareDto.Amount;

        if (sumOthers > expense.TotalAmount)
            throw new BadRequestException(
                $"Сумма долей ({sumOthers}) превышает общую сумму {expense.TotalAmount}");

        var remainder = expense.TotalAmount - sumOthers;
        var payerShare = await _db.ExpenseShares
            .FirstOrDefaultAsync(x => x.ExpenseId == expenseId && x.UserId == expense.PayerId);

        if (payerShare == null)
        {
            _db.ExpenseShares.Add(new ExpenseShare
            {
                ExpenseId = expenseId,
                UserId = expense.PayerId,
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

        if (share == null)
            return;

        if (share.Expense.GroupId != groupId)
        {
            throw new NotFoundException($"Expense {expenseId} not found in group {groupId}");
        }

        if (share.UserId == share.Expense.PayerId)
        {
            throw new BadRequestException("Payer share cannot be removed from an expense.");
        }

        _db.ExpenseShares.Remove(share);

        var expense = share.Expense;
        var sumOthers = await _db.ExpenseShares
            .Where(x => x.ExpenseId == expenseId && x.UserId != expense.PayerId)
            .SumAsync(x => x.Amount);

        var remainder = expense.TotalAmount - sumOthers;
        var payerShare = await _db.ExpenseShares
            .FirstOrDefaultAsync(x => x.ExpenseId == expenseId && x.UserId == expense.PayerId);

        if (payerShare == null)
        {
            _db.ExpenseShares.Add(new ExpenseShare
            {
                ExpenseId = expenseId,
                UserId = expense.PayerId,
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

    private async Task EnsureGroupMembersAsync(Guid groupId, IEnumerable<Guid> userIds)
    {
        var requiredUserIds = userIds.Distinct().ToArray();
        var memberCount = await _db.UserGroups
            .CountAsync(membership =>
                membership.GroupId == groupId && requiredUserIds.AsEnumerable().Contains(membership.UserId));

        if (memberCount != requiredUserIds.Length)
        {
            throw new BadRequestException("Payer and expense participants must be members of the group.");
        }
    }

    private static void EnsurePositiveAmount(decimal amount, string fieldName)
    {
        if (amount <= 0)
        {
            throw new BadRequestException($"{fieldName} must be positive.");
        }
    }

    private static void EnsureValidTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new BadRequestException("Expense title is required.");
        }
    }
}
