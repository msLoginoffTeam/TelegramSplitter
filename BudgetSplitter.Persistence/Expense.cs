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
    public Guid CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;

    [Required, MaxLength(300)]
    public string Title { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDraft { get; set; } = true;

    public ICollection<ExpenseShare> Shares { get; set; } = new List<ExpenseShare>();
}