using System.ComponentModel.DataAnnotations;

namespace Persistence;

public sealed class AuditLogEntry
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

    // No foreign key on purpose: deleting a group must not erase its history.
    public Guid? GroupId { get; set; }

    public long? ActorTelegramId { get; set; }

    [MaxLength(200)]
    public string? ActorDisplayName { get; set; }

    [MaxLength(100)]
    public string? ActorUsername { get; set; }

    [Required, MaxLength(120)]
    public string SubjectType { get; set; } = null!;

    [Required, MaxLength(20)]
    public string Operation { get; set; } = null!;

    [Required]
    public string EntityKeyJson { get; set; } = null!;

    public string? OldValuesJson { get; set; }

    public string? NewValuesJson { get; set; }
}
