namespace BelegErfassungApp.Services
{
    public interface ISettingsService
    {
        /// <summary>
        /// Löscht alle Receipts, ReceiptComments und AuditLogs aus der Datenbank.
        /// Benutzerdaten bleiben erhalten.
        /// </summary>
        Task<bool> ResetDatabaseAsync();

        /// <summary>
        /// Gibt die Anzahl der AuditLog-Einträge zurück.
        /// </summary>
        Task<int> GetAuditLogCountAsync();

        /// <summary>
        /// Exportiert alle AuditLog-Einträge als CSV-String.
        /// </summary>
        Task<string> ExportAuditLogsAsync();
    }
}
