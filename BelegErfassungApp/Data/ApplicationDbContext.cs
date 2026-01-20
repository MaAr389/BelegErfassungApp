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

        }
    }
}
