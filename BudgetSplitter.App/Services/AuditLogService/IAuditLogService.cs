using BudgetSplitter.Common.Dtos.Response;

namespace BudgetSplitter.App.Services.AuditLogService;

public interface IAuditLogService
{
    Task<AuditLogPageResponseDto> GetGroupAuditLogAsync(Guid groupId, int offset, int take);
}
