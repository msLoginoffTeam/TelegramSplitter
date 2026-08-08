using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Persistence;

public class Expense
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid GroupId { get; set; }
    public Group Group { get; set; } = null!;

    [Required]
    public Guid PayerId { get; set; }
    public User Payer { get; set; } = null!;

    [Required]
    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    [Required, MaxLength(300)]
    public string Title { get; set; } = null!;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDraft { get; set; } = true;

    public ICollection<ExpenseShare> Shares { get; set; } = new List<ExpenseShare>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
