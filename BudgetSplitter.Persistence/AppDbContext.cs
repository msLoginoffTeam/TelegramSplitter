using Microsoft.EntityFrameworkCore;
using System.Transactions;

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
        public DbSet<GroupMemberPermission> GroupMemberPermissions { get; set; } = null!;
        public DbSet<Expense> Expenses { get; set; } = null!;
        public DbSet<ExpenseShare> ExpenseShares { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<GroupInvite> GroupInvites { get; set; } = null!;
        public DbSet<AuditLogEntry> AuditLogEntries { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User
            modelBuilder.Entity<User>(b =>
            {
                b.HasKey(u => u.Id);
                b.HasIndex(u => u.TelegramId).IsUnique();
                b.HasIndex(u => u.Username);
            });

            // Group
            modelBuilder.Entity<Group>(b =>
            {
                b.HasKey(g => g.Id);
                b.Property(g => g.TelegramChatId);
                b
                    .HasOne(g => g.CreatedBy)
                    .WithMany(u => u.GroupsCreated)
                    .HasForeignKey(g => g.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);
                b
                    .HasOne(g => g.Owner)
                    .WithMany(u => u.GroupsOwned)
                    .HasForeignKey(g => g.OwnerId)
                    .OnDelete(DeleteBehavior.Restrict);
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

            modelBuilder.Entity<GroupMemberPermission>(b =>
            {
                b.HasKey(p => new { p.GroupId, p.UserId, p.Permission });
                b
                    .HasOne(p => p.Membership)
                    .WithMany(m => m.Permissions)
                    .HasForeignKey(p => new { p.UserId, p.GroupId })
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<GroupInvite>(b =>
            {
                b.HasKey(invite => invite.Id);
                b.HasIndex(invite => invite.TokenHash).IsUnique();

                b.HasOne(invite => invite.Group)
                    .WithMany()
                    .HasForeignKey(invite => invite.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(invite => invite.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(invite => invite.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AuditLogEntry>(b =>
            {
                b.HasKey(entry => entry.Id);
                b.Property(entry => entry.SubjectType).HasMaxLength(120);
                b.Property(entry => entry.Operation).HasMaxLength(20);
                b.Property(entry => entry.ActorDisplayName).HasMaxLength(200);
                b.Property(entry => entry.ActorUsername).HasMaxLength(100);
                b.HasIndex(entry => new { entry.GroupId, entry.OccurredAtUtc });
                b.HasIndex(entry => entry.OccurredAtUtc);
            });

            // Expense
            modelBuilder.Entity<Expense>(b =>
            {
                b.HasKey(e => e.Id);
                b.ToTable(table =>
                {
                    table.HasCheckConstraint("CK_Expenses_TotalAmount_Positive", "\"TotalAmount\" > 0");
                    table.HasCheckConstraint("CK_Expenses_Title_NotBlank", "length(btrim(\"Title\")) > 0");
                });

                b
                    .HasOne(e => e.Group)
                    .WithMany(g => g.Expenses)
                    .HasForeignKey(e => e.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                b
                    .HasOne(e => e.Payer)
                    .WithMany(u => u.ExpensesPaid)
                    .HasForeignKey(e => e.PayerId)
                    .OnDelete(DeleteBehavior.Restrict);
                b
                    .HasOne(e => e.CreatedByUser)
                    .WithMany(u => u.ExpensesCreated)
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ExpenseShare
            modelBuilder.Entity<ExpenseShare>(b =>
            {
                b.HasKey(es => es.Id);
                b.HasIndex(es => new { es.ExpenseId, es.UserId }).IsUnique();
                b.ToTable(table => table.HasCheckConstraint("CK_ExpenseShares_Amount_NonNegative", "\"Amount\" >= 0"));

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
                b.ToTable(table =>
                {
                    table.HasCheckConstraint("CK_Payments_Amount_Positive", "\"Amount\" > 0");
                    table.HasCheckConstraint("CK_Payments_DifferentUsers", "\"FromUserId\" <> \"ToUserId\"");
                });

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
                b
                    .HasOne(p => p.CreatedByUser)
                    .WithMany(u => u.PaymentsCreated)
                    .HasForeignKey(p => p.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
                b
                    .HasOne(p => p.Expense)
                    .WithMany(e => e.Payments)
                    .HasForeignKey(p => p.ExpenseId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            if (HasActiveTransaction())
            {
                return base.SaveChanges(acceptAllChangesOnSuccess);
            }

            var strategy = Database.CreateExecutionStrategy();
            return strategy.Execute(() =>
            {
                using var transaction = Database.BeginTransaction();
                var result = base.SaveChanges(acceptAllChangesOnSuccess);
                transaction.Commit();
                return result;
            });
        }

        public override Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            if (HasActiveTransaction())
            {
                return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            }

            var strategy = Database.CreateExecutionStrategy();
            return strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await Database.BeginTransactionAsync(cancellationToken);
                var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            });
        }

        private bool HasActiveTransaction()
            => Database.CurrentTransaction is not null || Transaction.Current is not null;
    }
}
