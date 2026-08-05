using BudgetSplitter.Common.Dtos.Request;
using BudgetSplitter.Common.Dtos.Response;
using BudgetSplitter.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace BudgetSplitter.App.Services.PaymentService;
    public class PaymentService : IPaymentService
    {
        private readonly AppDbContext _db;
        public PaymentService(AppDbContext db) => _db = db;

        public async Task<IEnumerable<PaymentResponseDto>> GetGroupPaymentsAsync(Guid groupId)
        {
            var payments = await _db.Payments
                .Where(p => p.GroupId == groupId)
                .AsNoTracking().Include(payment => payment.Expense!).Include(payment => payment.FromUser)
                .Include(payment => payment.ToUser)
                .ToListAsync();

            return payments.Select(p => new PaymentResponseDto
            {
                Id = p.Id,
                ExpenseId = p.ExpenseId,
                FromUserId = p.FromUserId,
                FromDisplayName = p.FromUser.DisplayName,
                FromUsername = p.FromUser.Username,
                ToUserId = p.ToUserId,
                ToDisplayName = p.ToUser.DisplayName,
                ToUsername = p.ToUser.Username,
                CreatedByUserId = p.CreatedByUserId,
                Amount = p.Amount,
                Timestamp = p.Timestamp
            });
        }

        public async Task<IEnumerable<PaymentResponseDto>> GetUserPaymentsAsync(Guid groupId, Guid userId)
        {
            var payments = await _db.Payments
                .Where(p => p.GroupId == groupId &&
                            (p.FromUserId == userId || p.ToUserId == userId))
                .AsNoTracking().Include(payment => payment.Expense).Include(payment => payment.FromUser)
                .Include(payment => payment.ToUser)
                .ToListAsync();

            return payments.Select(p => new PaymentResponseDto
            {
                Id = p.Id,
                ExpenseId = p.ExpenseId,
                FromUserId = p.FromUserId,
                FromDisplayName = p.FromUser.DisplayName,
                ToUserId = p.ToUserId,
                ToDisplayName = p.ToUser.DisplayName,
                CreatedByUserId = p.CreatedByUserId,
                Amount = p.Amount,
                Timestamp = p.Timestamp
            });
        }

        public async Task<PaymentResponseDto> CreatePaymentForExpenseAsync(
            Guid groupId,
            CreatePaymentForExpenseRequestDto dto,
            Guid createdByUserId)
        {
            EnsurePositiveAmount(dto.Amount);

            await using var transaction = await _db.Database.BeginTransactionAsync();
            await LockExpenseAsync(dto.ExpenseId);

            var expense = await _db.Expenses
                .Include(e => e.Shares)
                .FirstOrDefaultAsync(e => e.Id == dto.ExpenseId && e.GroupId == groupId)
                ?? throw new NotFoundException($"Expense {dto.ExpenseId} not found in group {groupId}");
        
            var share = expense.Shares.FirstOrDefault(s => s.UserId == dto.FromUserId)
                        ?? throw new BadRequestException(
                              $"User {dto.FromUserId} has no share in expense {dto.ExpenseId}");

            if (dto.FromUserId == expense.PayerId)
            {
                throw new BadRequestException("Payer cannot create a payment to themselves.");
            }
        
            var paidSum = await _db.Payments
                .Where(p => p.ExpenseId == dto.ExpenseId && p.FromUserId == dto.FromUserId)
                .SumAsync(p => p.Amount);
        
            if (paidSum + dto.Amount > share.Amount)
                throw new BadRequestException(
                    $"Payment ({dto.Amount}) exceeds remaining debt ({share.Amount - paidSum})");

            var payment = new Payment
            {
                GroupId = groupId,
                ExpenseId = expense.Id,
                FromUserId = dto.FromUserId,
                ToUserId = expense.PayerId,
                CreatedByUserId = createdByUserId,
                Amount = dto.Amount,
                Timestamp = DateTime.UtcNow
            };
            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        
            return new PaymentResponseDto
            {
                Id = payment.Id,
                ExpenseId = payment.ExpenseId,
                FromUserId = payment.FromUserId,
                ToUserId = payment.ToUserId,
                CreatedByUserId = payment.CreatedByUserId,
                Amount = payment.Amount,
                Timestamp = payment.Timestamp
            };
        }

        public async Task<PaymentResponseDto> CreateDirectPaymentAsync(
            Guid groupId,
            CreateDirectPaymentRequestDto dto,
            Guid createdByUserId)
        {
            EnsurePositiveAmount(dto.Amount);
            if (dto.FromUserId == dto.ToUserId)
            {
                throw new BadRequestException("Payment sender and recipient must be different users.");
            }

            var members = await _db.UserGroups
                .Where(ug => ug.GroupId == groupId && 
                            (ug.UserId == dto.FromUserId || ug.UserId == dto.ToUserId))
                .Select(ug => ug.UserId)
                .ToListAsync();
            if (!members.Contains(dto.FromUserId) || !members.Contains(dto.ToUserId))
                throw new BadRequestException("One or both users are not in the group");

            var payment = new Payment
            {
                GroupId = groupId,
                ExpenseId = null,
                FromUserId = dto.FromUserId,
                ToUserId = dto.ToUserId,
                CreatedByUserId = createdByUserId,
                Amount = dto.Amount,
                Timestamp = DateTime.UtcNow
            };
            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();

            return new PaymentResponseDto
            {
                Id = payment.Id,
                ExpenseId = payment.ExpenseId,
                FromUserId = payment.FromUserId,
                ToUserId = payment.ToUserId,
                CreatedByUserId = payment.CreatedByUserId,
                Amount = payment.Amount,
                Timestamp = payment.Timestamp
            };
        }

         public async Task UpdatePaymentAsync(
             Guid groupId,
             Guid paymentId,
             UpdatePaymentRequestDto dto)
         {
             EnsurePositiveAmount(dto.Amount);

             await using var transaction = await _db.Database.BeginTransactionAsync();

             var payment = await _db.Payments
                 .FirstOrDefaultAsync(p => p.Id == paymentId && p.GroupId == groupId)
                 ?? throw new NotFoundException($"Payment {paymentId} not found");

             if (payment.ExpenseId is { } expenseId)
             {
                 await LockExpenseAsync(expenseId);
                 await _db.Entry(payment).ReloadAsync();
                 var share = await _db.ExpenseShares
                     .FirstOrDefaultAsync(s => s.ExpenseId == expenseId && s.UserId == payment.FromUserId)
                     ?? throw new BadRequestException("Share not found");
        
                 var paidSum = await _db.Payments
                     .Where(p => p.ExpenseId == expenseId && p.FromUserId == payment.FromUserId)
                     .SumAsync(p => p.Amount);
        
                 var newSum = paidSum - payment.Amount + dto.Amount;
                 if (newSum > share.Amount)
                     throw new BadRequestException(
                         $"Updated payment ({dto.Amount}) exceeds remaining debt ({share.Amount - (paidSum - payment.Amount)})");
                 
             }
        
             payment.Amount = dto.Amount;
             await _db.SaveChangesAsync();
             await transaction.CommitAsync();
        }

        public async Task DeletePaymentAsync(Guid groupId, Guid paymentId)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            var payment = await _db.Payments
                .FirstOrDefaultAsync(p => p.Id == paymentId && p.GroupId == groupId);
            if (payment == null)
                return;

            if (payment.ExpenseId is { } expenseId)
            {
                await LockExpenseAsync(expenseId);
                await _db.Entry(payment).ReloadAsync();
            }

            _db.Payments.Remove(payment);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        private static void EnsurePositiveAmount(decimal amount)
        {
            if (amount <= 0)
            {
                throw new BadRequestException("Payment amount must be positive.");
            }
        }

        private async Task LockExpenseAsync(Guid expenseId)
        {
            await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Expenses\" WHERE \"Id\" = {expenseId} FOR UPDATE");
        }
    }
