using BudgetSplitter.Common.Dtos;
using BudgetSplitter.Common.Dtos.Response;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace BudgetSplitter.App.Services.BalanceService;

public class BalanceService : IBalanceService
{
    private readonly AppDbContext _db;
    public BalanceService(AppDbContext db) => _db = db;

    public async Task<BalanceResponseDto> GetBalancesAsync(Guid groupId)
    {
        var creditByCreator = await _db.Expenses
            .Where(e => e.GroupId == groupId)
            .Select(e => new
            {
                Creator = e.CreatedById,
                CreditPart = e.TotalAmount
                             - e.Shares.Single(s => s.UserId == e.CreatedById).Amount
            })
            .GroupBy(x => x.Creator)
            .Select(g => new { UserId = g.Key, Credit = g.Sum(x => x.CreditPart) })
            .ToListAsync();

        var owesByUser = await _db.ExpenseShares
            .Where(es => es.Expense.GroupId == groupId
                         && es.UserId != es.Expense.CreatedById)
            .GroupBy(es => es.UserId)
            .Select(g => new { UserId = g.Key, Owes = g.Sum(es => es.Amount) })
            .ToDictionaryAsync(x => x.UserId, x => x.Owes);

        var receivedByUser = await _db.Payments
            .Where(p => p.GroupId == groupId)
            .GroupBy(p => p.ToUserId)
            .Select(g => new { UserId = g.Key, Received = g.Sum(p => p.Amount) })
            .ToDictionaryAsync(x => x.UserId, x => x.Received);

        var sentByUser = await _db.Payments
            .Where(p => p.GroupId == groupId)
            .GroupBy(p => p.FromUserId)
            .Select(g => new { UserId = g.Key, Sent = g.Sum(p => p.Amount) })
            .ToDictionaryAsync(x => x.UserId, x => x.Sent);

        var users = await _db.UserGroups
            .Where(ug => ug.GroupId == groupId)
            .Select(ug => ug.User)
            .ToListAsync();

        var result = new BalanceResponseDto();
        foreach (var u in users)
        {
            var credit = creditByCreator.FirstOrDefault(x => x.UserId == u.Id)?.Credit ?? 0m;
            var owes = owesByUser.GetValueOrDefault(u.Id, 0m);
            var received = receivedByUser.GetValueOrDefault(u.Id, 0m);
            var sent = sentByUser.GetValueOrDefault(u.Id, 0m);

            result.Balances.Add(new UserBalanceResponseDto
            {
                UserId = u.Id,
                DisplayName = u.DisplayName,
                Balance = credit - owes - received + sent
            });
        }

        return result;
    }

    public async Task<TransferSuggestionsResponseDto> GetSuggestedTransfersAsync(Guid groupId, bool useNp)
    {
        var balanceDto = await GetBalancesAsync(groupId);
        var list = balanceDto.Balances
            .Select(b => new
            {
                UserId = b.UserId,
                DisplayName = b.DisplayName,
                Balance = Math.Round(b.Balance, 2)
            })
            .Where(x => x.Balance != 0)
            .ToList();

        var debtors = new Queue<(Guid user, decimal amount, string? name)>(
            list.Where(x => x.Balance < 0)
                .Select(x => (x.UserId, -x.Balance, x.DisplayName))); // amount > 0
        var creditors = new Queue<(Guid user, decimal amount, string? name)>(
            list.Where(x => x.Balance > 0)
                .Select(x => (x.UserId, x.Balance, x.DisplayName))); // amount > 0

        var suggestions = new List<TransferSuggestionDto>();
        
        while (debtors.Any() && creditors.Any())
        {
            var (debtor, oweAmt, debtorName) = debtors.Dequeue();
            var (creditor, credAmt, creditorName) = creditors.Dequeue();

            var transfer = Math.Min(oweAmt, credAmt);

            suggestions.Add(new TransferSuggestionDto
            {
                FromUserId = debtor,
                ToUserId = creditor,
                Amount = transfer,
                FromUserName = debtorName,
                ToUserName = creditorName
            });

            if (oweAmt > transfer)
                debtors.Enqueue((debtor, oweAmt - transfer, debtorName));
            if (credAmt > transfer)
                creditors.Enqueue((creditor, credAmt - transfer, creditorName));
        }

        return new TransferSuggestionsResponseDto
        {
            Transfers = suggestions
        };
    }
    
    public Task<IEnumerable<OperationHistoryResponseDto>> GetHistoryAsync(Guid groupId)
    {
        throw new NotImplementedException();
    }
    //
    // public async Task<IEnumerable<OperationHistoryResponseDto>> GetHistoryAsync(Guid groupId)
    // {
    //     // объединить расходы и платежи в один список, отсортировать по Timestamp
    //     var expenses = await _db.Expenses
    //         .Where(e => e.GroupId == groupId && !e.IsDraft)
    //         .Select(e => new OperationHistoryResponseDto {
    //             Id = e.Id,
    //             Type = "Expense",
    //             InitiatorUserId = e.CreatedById,
    //             Timestamp = e.CreatedAt,
    //             Description = $"Expense '{e.Title}' {e.TotalAmount}₽"
    //         }).ToListAsync();
    //
    //     var payments = await _db.Payments
    //         .Where(p => p.GroupId == groupId)
    //         .Select(p => new OperationHistoryResponseDto {
    //             Id = p.Id,
    //             Type = p.ExpenseId != null ? "PaymentForExpense" : "DirectPayment",
    //             InitiatorUserId = p.FromUserId,
    //             Timestamp = p.Timestamp,
    //             Description = p.ExpenseId != null
    //                 ? $"Paid {p.Amount}₽ for expense {p.ExpenseId}"
    //                 : $"Transferred {p.Amount}₽ to user {p.ToUserId}"
    //         }).ToListAsync();
    //
    //     return expenses
    //         .Concat(payments)
    //         .OrderBy(x => x.Timestamp);
    // }
}