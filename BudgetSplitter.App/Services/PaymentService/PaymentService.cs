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
                ExpenseId = p.Expense?.Id,
                FromUserId = p.FromUserId,
                FromUserName = p.FromUser.DisplayName,
                ToUserId = p.ToUserId,
                ToUserName = p.ToUser.DisplayName,
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
                ExpenseId = p.Expense?.Id,
                FromUserId = p.FromUserId,
                FromUserName = p.FromUser.DisplayName,
                ToUserId = p.ToUserId,
                ToUserName = p.ToUser.DisplayName,
                Amount = p.Amount,
                Timestamp = p.Timestamp
            });
        }

        public async Task<PaymentResponseDto> CreatePaymentForExpenseAsync(
            Guid groupId,
            CreatePaymentForExpenseRequestDto dto)
        {
            var expense = await _db.Expenses
                .Include(e => e.Shares)
                .FirstOrDefaultAsync(e => e.Id == dto.ExpenseId && e.GroupId == groupId)
                ?? throw new NotFoundException($"Expense {dto.ExpenseId} not found in group {groupId}");
        
            var share = expense.Shares.FirstOrDefault(s => s.UserId == dto.FromUserId)
                        ?? throw new BadRequestException(
                              $"User {dto.FromUserId} has no share in expense {dto.ExpenseId}");
        
            var paidSum = await _db.Payments
                .Where(p => p.Expense != null && p.Expense.Id == dto.ExpenseId && p.FromUserId == dto.FromUserId)
                .SumAsync(p => p.Amount);
        
            if (paidSum + dto.Amount > share.Amount)
                throw new BadRequestException(
                    $"Payment ({dto.Amount}) exceeds remaining debt ({share.Amount - paidSum})");

            if (paidSum + dto.Amount == share.Amount)
            {
                share.IsPaid = true;
            }
        
            var payment = new Payment
            {
                GroupId = groupId,
                Expense =  expense,
                FromUserId = dto.FromUserId,
                ToUserId = expense.CreatedById,
                Amount = dto.Amount,
                Timestamp = DateTime.UtcNow
            };
            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();
        
            return new PaymentResponseDto
            {
                Id = payment.Id,
                ExpenseId = payment.Expense.Id,
                FromUserId = payment.FromUserId,
                ToUserId = payment.ToUserId,
                Amount = payment.Amount,
                Timestamp = payment.Timestamp
            };
        }

        public async Task<PaymentResponseDto> CreateDirectPaymentAsync(
            Guid groupId,
            CreateDirectPaymentRequestDto dto)
        {
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
                Expense = null,
                FromUserId = dto.FromUserId,
                ToUserId = dto.ToUserId,
                Amount = dto.Amount,
                Timestamp = DateTime.UtcNow
            };
            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();

            return new PaymentResponseDto
            {
                Id = payment.Id,
                ExpenseId = payment.Expense?.Id,
                FromUserId = payment.FromUserId,
                ToUserId = payment.ToUserId,
                Amount = payment.Amount,
                Timestamp = payment.Timestamp
            };
        }

         public async Task UpdatePaymentAsync(
             Guid groupId,
             Guid paymentId,
             UpdatePaymentRequestDto dto)
         {
             var payment = await _db.Payments
                 .Include(p => p.Expense)
                 .FirstOrDefaultAsync(p => p.Id == paymentId && p.GroupId == groupId)
                 ?? throw new NotFoundException($"Payment {paymentId} not found");
        
             if (payment.Expense != null)
             {
                 var share = await _db.ExpenseShares
                     .FirstOrDefaultAsync(s => s.ExpenseId == payment.Expense.Id && s.UserId == payment.FromUserId)
                     ?? throw new BadRequestException("Share not found");
        
                 var paidSum = await _db.Payments
                     .Where(p => p.Expense != null && p.Expense.Id == payment.Expense.Id && p.FromUserId == payment.FromUserId)
                     .SumAsync(p => p.Amount);
        
                 var newSum = paidSum - payment.Amount + dto.Amount;
                 if (newSum > share.Amount)
                     throw new BadRequestException(
                         $"Updated payment ({dto.Amount}) exceeds remaining debt ({share.Amount - (paidSum - payment.Amount)})");
                 
                 if (newSum == share.Amount)
                 {
                     share.IsPaid = true;
                 }
                 
             }
        
             payment.Amount = dto.Amount;
             await _db.SaveChangesAsync();
        }

        public async Task DeletePaymentAsync(Guid groupId, Guid paymentId)
        {
            var payment = await _db.Payments
                .FirstOrDefaultAsync(p => p.Id == paymentId && p.GroupId == groupId);
            if (payment == null)
                return;

            _db.Payments.Remove(payment);
            await _db.SaveChangesAsync();
        }
    }