using BudgetSplitter.App.Authorization;
using BudgetSplitter.App.Services.AuditLogService;
using BudgetSplitter.Common.Authorization;
using BudgetSplitter.Common.Dtos.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetSplitter.App.Controllers;

[ApiController]
[Authorize]
[Route("api/groups/{groupId:guid}/audit-log")]
public sealed class AuditLogController(IAuditLogService auditLog) : ControllerBase
{
    /// <summary>
    /// Returns the newest audit events for a group.
    /// </summary>
    [HttpGet]
    [RequireGroupPermission(GroupPermission.ViewGroup)]
    public async Task<ActionResult<AuditLogPageResponseDto>> GetAuditLog(
        Guid groupId,
        [FromQuery] int offset = 0,
        [FromQuery] int take = 30)
        => Ok(await auditLog.GetGroupAuditLogAsync(groupId, offset, take));
}
