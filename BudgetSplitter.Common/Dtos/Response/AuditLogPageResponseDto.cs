namespace BudgetSplitter.Common.Dtos.Response;

public sealed class AuditLogPageResponseDto
{
    public IReadOnlyList<AuditLogEntryResponseDto> Entries { get; set; } = [];
    public bool HasMore { get; set; }
    public int? NextOffset { get; set; }
}
