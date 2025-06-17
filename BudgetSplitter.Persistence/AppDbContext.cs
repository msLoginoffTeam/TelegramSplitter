using Microsoft.EntityFrameworkCore;

namespace Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Group> Groups { get; set; } = null!;
        public DbSet<UserGroup> UserGroups { get; set; } = null!;
        public DbSet<Expense> Expenses { get; set; } = null!;
        public DbSet<ExpenseShare> ExpenseShares { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User
            modelBuilder.Entity<User>(b =>
            {
                b.HasKey(u => u.Id);
                b.HasIndex(u => u.TelegramId).IsUnique();
            });

            // Group
            modelBuilder.Entity<Group>(b =>
            {
                b.HasKey(g => g.Id);
                b.Property(g => g.TelegramChatId);
            });

            // UserGroup (many-to-many)
            modelBuilder.Entity<UserGroup>(b =>
            {
                b.HasKey(ug => new { ug.UserId, ug.GroupId });
                b
                    .HasOne(ug => ug.User)
                    .WithMany(u => u.UserGroups)
                    .HasForeignKey(ug => ug.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                b
                    .HasOne(ug => ug.Group)
                    .WithMany(g => g.UserGroups)
                    .HasForeignKey(ug => ug.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Expense
            modelBuilder.Entity<Expense>(b =>
            {
                b.HasKey(e => e.Id);

                b
                    .HasOne(e => e.Group)
                    .WithMany(g => g.Expenses)
                    .HasForeignKey(e => e.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                b
                    .HasOne(e => e.CreatedBy)
                    .WithMany(u => u.ExpensesCreated)
                    .HasForeignKey(e => e.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ExpenseShare
            modelBuilder.Entity<ExpenseShare>(b =>
            {
                b.HasKey(es => es.Id);

                b
                    .HasOne(es => es.Expense)
                    .WithMany(e => e.Shares)
                    .HasForeignKey(es => es.ExpenseId)
                    .OnDelete(DeleteBehavior.Cascade);

                b
                    .HasOne(es => es.User)
                    .WithMany(u => u.Shares)
                    .HasForeignKey(es => es.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Payment
            modelBuilder.Entity<Payment>(b =>
            {
                b.HasKey(p => p.Id);

                b
                    .HasOne(p => p.Group)
                    .WithMany(g => g.Payments)
                    .HasForeignKey(p => p.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                b
                    .HasOne(p => p.FromUser)
                    .WithMany(u => u.PaymentsSent)
                    .HasForeignKey(p => p.FromUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                b
                    .HasOne(p => p.ToUser)
                    .WithMany(u => u.PaymentsReceived)
                    .HasForeignKey(p => p.ToUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}