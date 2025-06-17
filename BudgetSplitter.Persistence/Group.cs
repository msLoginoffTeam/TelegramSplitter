using System.ComponentModel.DataAnnotations;

namespace Persistence;

public class Group
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)]
    public string Title { get; set; } = null!;
    public long? TelegramChatId { get; set; }

    public ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}