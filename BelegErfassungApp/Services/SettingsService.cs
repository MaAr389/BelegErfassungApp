using BelegErfassungApp.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Linq;

namespace BelegErfassungApp.Services
{
    public class SettingsService
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
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Lösche alle ReceiptComments (muss zuerst wegen Foreign Key)
                var comments = await context.ReceiptComments.ToListAsync();
                context.ReceiptComments.RemoveRange(comments);
                _logger.LogInformation($"Deleting {comments.Count} receipt comments...");

                // Lösche alle Receipts
                var receipts = await context.Receipts.ToListAsync();
                context.Receipts.RemoveRange(receipts);
                _logger.LogInformation($"Deleting {receipts.Count} receipts...");

                // Lösche alle AuditLogs
                var auditLogs = await context.AuditLogs.ToListAsync();
                context.AuditLogs.RemoveRange(auditLogs);
                _logger.LogInformation($"Deleting {auditLogs.Count} audit log entries...");

                // Speichere alle Änderungen
                await context.SaveChangesAsync();

                _logger.LogWarning("Database reset completed successfully. All receipts, comments, and audit logs have been deleted.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while resetting database");
                return false;
            }
        }

        /// <summary>
        /// Gibt die Anzahl der AuditLog-Einträge zurück.
        /// </summary>
        public async Task<int> GetAuditLogCountAsync()
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                return await context.AuditLogs.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting audit log count");
                return 0;
            }
        }

        /// <summary>
        /// Exportiert alle AuditLog-Einträge als CSV-String.
        /// </summary>
        public async Task<string> ExportAuditLogsAsync()
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var logs = await context.AuditLogs
                    .OrderByDescending(x => x.TimestampUtc)
                    .ToListAsync();

                if (!logs.Any())
                {
                    return string.Empty;
                }

                var csv = new StringBuilder();

                // CSV Header
                csv.AppendLine("Id;TimestampUtc;ActorUserId;ActorEmail;Action;EntityType;EntityId;Description");

                // CSV Data
                foreach (var log in logs)
                {
                    csv.AppendLine($"\"{log.Id}\";\"{log.TimestampUtc:yyyy-MM-dd HH:mm:ss}\";\"{log.ActorUserId ?? ""}\";" +
                                   $"\"{EscapeCsv(log.ActorEmail ?? "")}\";\"{EscapeCsv(log.Action)}\";\"{EscapeCsv(log.EntityType)}\";" +
                                   $"\"{log.EntityId ?? ""}\";" +
                                   $"\"{EscapeCsv(log.Description ?? "")}\"");
                }

                _logger.LogInformation($"Exported {logs.Count} audit log entries to CSV");
                return csv.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while exporting audit logs");
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
