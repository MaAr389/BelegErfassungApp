using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BelegErfassungApp.Data;

namespace BelegErfassungApp.Services
{
    public interface IAuditLogService
    {
        /// <summary>
        /// Protokolliert eine Aktion im Audit-Log
        /// </summary>
        Task LogAsync(
            string action,
            string entityType,
            string entityId,
            string actorUserId,
            string? actorEmail = null,
            string? targetUserId = null,
            string? detailsJson = null,
            string? description = null,
            string? ipAddress = null);

        /// <summary>
        /// Holt alle Audit-Log-Einträge (mit optionalen Filtern)
        /// </summary>
        Task<List<AuditLogEntry>> GetAuditLogsAsync(
            string? filterAction = null,
            string? filterUserId = null,
            string? filterEntityId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int skip = 0,
            int take = 50);

        /// <summary>
        /// Holt die Audit-Logs für einen spezifischen Beleg
        /// </summary>
        Task<List<AuditLogEntry>> GetLogsForReceiptAsync(int receiptId);

        /// <summary>
        /// Holt die Audit-Logs für einen spezifischen Benutzer (als Actor)
        /// </summary>
        Task<List<AuditLogEntry>> GetLogsForUserAsync(string userId);

        /// <summary>
        /// Gibt die Gesamtanzahl der Audit-Log-Einträge zurück
        /// </summary>
        Task<int> GetTotalCountAsync();
    }
}
