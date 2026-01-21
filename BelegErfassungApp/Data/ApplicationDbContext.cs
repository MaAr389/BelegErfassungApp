using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace BelegErfassungApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Receipt> Receipts { get; set; }

        // Am Anfang der Klasse (in OnModelCreating):
        public DbSet<AuditLogEntry> AuditLogs { get; set; }

        public DbSet<ReceiptComment> ReceiptComments { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Receipt Konfiguration
            builder.Entity<Receipt>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Receipt>()
                .HasIndex(r => r.UserId);

            builder.Entity<Receipt>()
                .HasIndex(r => r.Status);

            builder.Entity<Receipt>()
                .Property(r => r.ManualPrice)
                .HasPrecision(18, 2);

            builder.Entity<Receipt>()
                .Property(r => r.OcrGrossAmount)
                .HasPrecision(18, 2);

            builder.Entity<Receipt>()
                .Property(r => r.OcrNetAmount)
                .HasPrecision(18, 2);

            builder.Entity<Receipt>()
                .Property(r => r.OcrVatAmount)
                .HasPrecision(18, 2);

            // In OnModelCreating() Methode, z.B. nach Receipt-Konfiguration:
            builder.Entity<AuditLogEntry>()
                .HasKey(a => a.Id);

            // Index für bessere Abfrage-Performance
            builder.Entity<AuditLogEntry>()
                .HasIndex(a => a.TimestampUtc)
                .IsDescending();

            builder.Entity<AuditLogEntry>()
                .HasIndex(a => a.ActorUserId);

            builder.Entity<AuditLogEntry>()
                .HasIndex(a => a.EntityId);

            builder.Entity<AuditLogEntry>()
                .HasIndex(a => new { a.EntityType, a.Action });

            builder.Entity<AuditLogEntry>()
                .Property(a => a.TimestampUtc)
                .HasDefaultValueSql("GETUTCDATE()");  // SQL Server: verwendet die Datenbank-Zeit

            // ReceiptComment Konfiguration
            builder.Entity<ReceiptComment>()
                .HasOne(rc => rc.Receipt)
                .WithMany()
                .HasForeignKey(rc => rc.ReceiptId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ReceiptComment>()
                .HasOne(rc => rc.User)
                .WithMany()
                .HasForeignKey(rc => rc.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ReceiptComment>()
                .HasOne(rc => rc.ParentComment)
                .WithMany()
                .HasForeignKey(rc => rc.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Index für Performance
            builder.Entity<ReceiptComment>()
                .HasIndex(rc => rc.ReceiptId);

            builder.Entity<ReceiptComment>()
                .HasIndex(rc => rc.CreatedAt);

        }
    }
}
