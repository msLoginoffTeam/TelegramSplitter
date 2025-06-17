using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Persistence;

public class Payment
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid GroupId { get; set; }
    public Group Group { get; set; } = null!;

    [Required]
    public Guid FromUserId { get; set; }
    public User FromUser { get; set; } = null!;

    [Required]
    public Guid ToUserId { get; set; }
    public User ToUser { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}