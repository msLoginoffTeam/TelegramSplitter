namespace BudgetSplitter.Common.Dtos.Response;

public sealed class AuditLogEntryResponseDto
{
    public Guid Id { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public long? ActorTelegramId { get; set; }
    public string? ActorDisplayName { get; set; }
    public string? ActorUsername { get; set; }
    public string SubjectType { get; set; } = null!;
    public string Operation { get; set; } = null!;
    public string EntityKeyJson { get; set; } = null!;
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
}
