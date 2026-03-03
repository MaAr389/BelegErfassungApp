using BelegErfassungApp.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Linq;

namespace BelegErfassungApp.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<SettingsService> _logger;

        public SettingsService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            ILogger<SettingsService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        /// <summary>
        /// Löscht alle Receipts, ReceiptComments und AuditLogs aus der Datenbank.
        /// Benutzerdaten bleiben erhalten.
        /// </summary>

        public async Task<bool> ResetDatabaseAsync()
        {
            _logger.LogWarning("🗑️ ResetDatabaseAsync STARTED");

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // SUPER SIMPEL - nur SQL, keine Entity Framework Komplexität
                await context.Database.ExecuteSqlRawAsync("DELETE FROM ReceiptComments");
                await context.Database.ExecuteSqlRawAsync("DELETE FROM Receipts");
                await context.Database.ExecuteSqlRawAsync("DELETE FROM AuditLogs");
                await context.Database.ExecuteSqlRawAsync("DELETE FROM MemberApplications");

                _logger.LogWarning("✅ ResetDatabaseAsync SUCCESS - SQL executed");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ ResetDatabaseAsync ERROR: {Message}", ex.Message);
                return false;
            }
        }



        /// <summary>
        /// Löscht alle Receipts, ReceiptComments und AuditLogs aus der Datenbank.
        /// Benutzerdaten bleiben erhalten.
        /// </summary>


        /// <summary>
        /// Gibt die Anzahl der AuditLog-Einträge zurück.
        /// </summary>
        public async Task<int> GetAuditLogCountAsync()
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var count = await context.AuditLogs.CountAsync();
                _logger.LogDebug("📊 Current audit log count: {Count}", count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error occurred while getting audit log count: {Message}", ex.Message);
                return 0;
            }
        }

        /// <summary>
        /// Exportiert alle AuditLog-Einträge als CSV-String.
        /// </summary>
        public async Task<string> ExportAuditLogsAsync()
        {
            _logger.LogInformation("📥 Starting audit log export...");

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var logs = await context.AuditLogs
                    .OrderByDescending(x => x.TimestampUtc)
                    .ToListAsync();

                _logger.LogInformation("📊 Found {Count} logs to export", logs.Count);

                if (!logs.Any())
                {
                    _logger.LogWarning("⚠️ No audit logs available for export");
                    return string.Empty;
                }

                var csv = new StringBuilder();

                // CSV Header
                csv.AppendLine("Id;TimestampUtc;ActorUserId;ActorEmail;Action;EntityType;EntityId;Description");

                // CSV Data
                int lineCount = 0;
                foreach (var log in logs)
                {
                    csv.AppendLine($"\"{log.Id}\";\"{log.TimestampUtc:yyyy-MM-dd HH:mm:ss}\";\"{log.ActorUserId ?? ""}\";\"" +
                        $"{EscapeCsv(log.ActorEmail ?? "")}\";\"{EscapeCsv(log.Action)}\";\"{EscapeCsv(log.EntityType)}\";" +
                        $"\"{log.EntityId ?? ""}\";\"" +
                        $"{EscapeCsv(log.Description ?? "")}\"");
                    lineCount++;
                }

                _logger.LogInformation("✅ Exported {Count} audit log entries to CSV ({SizeKB} KB)",
                    lineCount, csv.Length / 1024);

                return csv.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ Error occurred while exporting audit logs: {Message}\n   InnerException: {InnerMessage}",
                    ex.Message, ex.InnerException?.Message ?? "keine");
                throw;
            }
        }

        /// <summary>
        /// Escapes special characters for CSV format.
        /// </summary>
        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            // Ersetze doppelte Anführungszeichen mit zwei doppelten Anführungszeichen
            return value.Replace("\"", "\"\"");
        }
    }
}
