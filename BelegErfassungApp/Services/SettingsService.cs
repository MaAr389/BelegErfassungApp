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
        //public async Task<bool> ResetDatabaseAsync()
        //{
        //    try
        //    {
        //        using var context = await _contextFactory.CreateDbContextAsync();

        //        // Lösche alle ReceiptComments (muss zuerst wegen Foreign Key)
        //        var comments = await context.ReceiptComments.ToListAsync();
        //        context.ReceiptComments.RemoveRange(comments);
        //        _logger.LogInformation($"Deleting {comments.Count} receipt comments...");

        //        // Lösche alle Receipts
        //        var receipts = await context.Receipts.ToListAsync();
        //        context.Receipts.RemoveRange(receipts);
        //        _logger.LogInformation($"Deleting {receipts.Count} receipts...");

        //        // Lösche alle AuditLogs
        //        var auditLogs = await context.AuditLogs.ToListAsync();
        //        context.AuditLogs.RemoveRange(auditLogs);
        //        _logger.LogInformation($"Deleting {auditLogs.Count} audit log entries...");

        //        // Speichere alle Änderungen
        //        await context.SaveChangesAsync();

        //        _logger.LogWarning("Database reset completed successfully. All receipts, comments, and audit logs have been deleted.");
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error occurred while resetting database");
        //        return false;
        //    }
        //}


        /// <summary>
        /// Löscht alle Receipts, ReceiptComments und AuditLogs aus der Datenbank.
        /// Benutzerdaten bleiben erhalten.
        /// </summary>
        //public async Task<bool> ResetDatabaseAsync()
        //{
        //    _logger.LogWarning("🗑️ === DATABASE RESET STARTED ===");

        //    try
        //    {
        //        using var context = await _contextFactory.CreateDbContextAsync();
        //        _logger.LogInformation("✅ Database context created");

        //        // Schritt 1: ReceiptComments löschen (muss zuerst wegen Foreign Key zu Receipts)
        //        try
        //        {
        //            var comments = await context.ReceiptComments.ToListAsync();
        //            _logger.LogInformation("📋 Found {Count} receipt comments to delete", comments.Count);

        //            if (comments.Count > 0)
        //            {
        //                context.ReceiptComments.RemoveRange(comments);
        //                _logger.LogDebug("ReceiptComments marked for deletion");
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, "❌ Error loading receipt comments: {Message}", ex.Message);
        //            throw;
        //        }

        //        // Schritt 2: Receipts löschen
        //        try
        //        {
        //            var receipts = await context.Receipts.ToListAsync();
        //            _logger.LogInformation("📋 Found {Count} receipts to delete", receipts.Count);

        //            if (receipts.Count > 0)
        //            {
        //                context.Receipts.RemoveRange(receipts);
        //                _logger.LogDebug("Receipts marked for deletion");
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, "❌ Error loading receipts: {Message}", ex.Message);
        //            throw;
        //        }

        //        // Schritt 3: AuditLogs löschen
        //        try
        //        {
        //            var auditLogs = await context.AuditLogs.ToListAsync();
        //            _logger.LogInformation("📋 Found {Count} audit log entries to delete", auditLogs.Count);

        //            if (auditLogs.Count > 0)
        //            {
        //                context.AuditLogs.RemoveRange(auditLogs);
        //                _logger.LogDebug("AuditLogs marked for deletion");
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, "❌ Error loading audit logs: {Message}", ex.Message);
        //            throw;
        //        }

        //        // Schritt 4: Speichere alle Änderungen
        //        _logger.LogInformation("💾 Saving all changes to database...");
        //        try
        //        {
        //            var rowsAffected = await context.SaveChangesAsync();
        //            _logger.LogWarning("✅ Database reset completed successfully! {RowsAffected} rows deleted", rowsAffected);
        //            return true;
        //        }
        //        catch (DbUpdateException dbEx)
        //        {
        //            _logger.LogError(dbEx,
        //                "❌ Database constraint violation while saving: {Message}\n   InnerException: {InnerMessage}\n   This usually means Foreign Key constraints are preventing deletion.",
        //                dbEx.Message, dbEx.InnerException?.Message ?? "keine");
        //            throw;
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex,
        //                "❌ Database error while saving changes: {Message}\n   InnerException: {InnerMessage}",
        //                ex.Message, ex.InnerException?.Message ?? "keine");
        //            throw;
        //        }
        //    }
        //    catch (DbUpdateConcurrencyException concEx)
        //    {
        //        _logger.LogError(concEx,
        //            "❌ Concurrency error during database reset: {Message}\n   Another process may have modified the data",
        //            concEx.Message);
        //        return false;
        //    }
        //    catch (InvalidOperationException invEx)
        //    {
        //        _logger.LogError(invEx,
        //            "❌ Invalid operation during database reset: {Message}\n   Check if database connection is available",
        //            invEx.Message);
        //        return false;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex,
        //            "❌ Unexpected error during database reset: {Message}\n   Type: {ExceptionType}\n   InnerException: {InnerMessage}",
        //            ex.Message, ex.GetType().FullName, ex.InnerException?.Message ?? "keine");
        //        return false;
        //    }
        //    finally
        //    {
        //        _logger.LogWarning("🗑️ === DATABASE RESET FINISHED ===");
        //    }
        //}

        public async Task<bool> ResetDatabaseAsync()
        {
            _logger.LogWarning("🗑️ === DATABASE RESET STARTED ===");

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                _logger.LogInformation("✅ Database context created");

                // WICHTIG: Reihenfolge ist entscheidend wegen Foreign Keys!
                // 1. Zuerst AuditLogs (Constraint-free)
                // 2. Dann ReceiptComments (FK zu Receipts mit Restrict)
                // 3. Dann Receipts

                // Schritt 1: AuditLogs löschen (keine Foreign Keys)
                try
                {
                    var auditLogs = await context.AuditLogs.ToListAsync();
                    _logger.LogInformation("📋 Found {Count} audit log entries to delete", auditLogs.Count);

                    if (auditLogs.Count > 0)
                    {
                        context.AuditLogs.RemoveRange(auditLogs);
                        _logger.LogDebug("AuditLogs marked for deletion");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error loading audit logs: {Message}", ex.Message);
                    throw;
                }

                // Schritt 2: ReceiptComments löschen (FK zu Receipts mit Restrict)
                try
                {
                    var comments = await context.ReceiptComments.ToListAsync();
                    _logger.LogInformation("📋 Found {Count} receipt comments to delete", comments.Count);

                    if (comments.Count > 0)
                    {
                        context.ReceiptComments.RemoveRange(comments);
                        _logger.LogDebug("ReceiptComments marked for deletion");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error loading receipt comments: {Message}", ex.Message);
                    throw;
                }

                // Schritt 3: Receipts löschen
                try
                {
                    var receipts = await context.Receipts.ToListAsync();
                    _logger.LogInformation("📋 Found {Count} receipts to delete", receipts.Count);

                    if (receipts.Count > 0)
                    {
                        context.Receipts.RemoveRange(receipts);
                        _logger.LogDebug("Receipts marked for deletion");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error loading receipts: {Message}", ex.Message);
                    throw;
                }

                // Schritt 4: Speichere alle Änderungen
                _logger.LogInformation("💾 Saving all changes to database...");
                try
                {
                    var rowsAffected = await context.SaveChangesAsync();
                    _logger.LogWarning("✅ Database reset completed successfully! {RowsAffected} rows deleted", rowsAffected);
                    return true;
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx,
                        "❌ Database constraint violation while saving: {Message}\n   This is likely due to foreign key constraints",
                        dbEx.Message);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Unexpected error during database reset: {Message}", ex.Message);
                return false;
            }
        }




        /// <summary>
        /// Gibt die Anzahl der AuditLog-Einträge zurück.
        /// </summary>
        //public async Task<int> GetAuditLogCountAsync()
        //{
        //    try
        //    {
        //        using var context = await _contextFactory.CreateDbContextAsync();
        //        return await context.AuditLogs.CountAsync();
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error occurred while getting audit log count");
        //        return 0;
        //    }
        //}

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
        //public async Task<string> ExportAuditLogsAsync()
        //{
        //    try
        //    {
        //        using var context = await _contextFactory.CreateDbContextAsync();
        //        var logs = await context.AuditLogs
        //            .OrderByDescending(x => x.TimestampUtc)
        //            .ToListAsync();

        //        if (!logs.Any())
        //        {
        //            return string.Empty;
        //        }

        //        var csv = new StringBuilder();

        //        // CSV Header
        //        csv.AppendLine("Id;TimestampUtc;ActorUserId;ActorEmail;Action;EntityType;EntityId;Description");

        //        // CSV Data
        //        foreach (var log in logs)
        //        {
        //            csv.AppendLine($"\"{log.Id}\";\"{log.TimestampUtc:yyyy-MM-dd HH:mm:ss}\";\"{log.ActorUserId ?? ""}\";" +
        //                           $"\"{EscapeCsv(log.ActorEmail ?? "")}\";\"{EscapeCsv(log.Action)}\";\"{EscapeCsv(log.EntityType)}\";" +
        //                           $"\"{log.EntityId ?? ""}\";" +
        //                           $"\"{EscapeCsv(log.Description ?? "")}\"");
        //        }

        //        _logger.LogInformation($"Exported {logs.Count} audit log entries to CSV");
        //        return csv.ToString();
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error occurred while exporting audit logs");
        //        throw;
        //    }
        //}


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
