using System.ComponentModel.DataAnnotations;

namespace Persistence;

public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public long TelegramId { get; set; }

    [MaxLength(200)]
    public string? DisplayName { get; set; }

    [MaxLength(100)]
    public string? Username { get; set; }

    public ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
    public ICollection<Group> GroupsCreated { get; set; } = new List<Group>();
    public ICollection<Group> GroupsOwned { get; set; } = new List<Group>();
    public ICollection<Expense> ExpensesPaid { get; set; } = new List<Expense>();
    public ICollection<Expense> ExpensesCreated { get; set; } = new List<Expense>();
    public ICollection<ExpenseShare> Shares { get; set; } = new List<ExpenseShare>();
    public ICollection<Payment> PaymentsSent { get; set; } = new List<Payment>();
    public ICollection<Payment> PaymentsReceived { get; set; } = new List<Payment>();
    public ICollection<Payment> PaymentsCreated { get; set; } = new List<Payment>();
}
