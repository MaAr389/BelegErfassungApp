using System;

namespace BelegErfassungApp.Data
{
    public class AuditLogEntry
    {
        public int Id { get; set; }

        /// <summary>
        /// Zeitstempel der Aktion (UTC)
        /// </summary>
        public DateTime TimestampUtc { get; set; }

        /// <summary>
        /// Art der Aktion: "ReceiptCreated", "StatusChanged", "ReceiptDeleted", "OcrCompleted"
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Typ der Entität: "Receipt", "User"
        /// </summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// ID der betroffenen Entität (z.B. ReceiptId)
        /// </summary>
        public string EntityId { get; set; } = string.Empty;

        /// <summary>
        /// UserId des Handelnden (Wer hat die Aktion ausgelöst)
        /// </summary>
        public string ActorUserId { get; set; } = string.Empty;

        /// <summary>
        /// Email des Actors (für bessere Lesbarkeit)
        /// </summary>
        public string? ActorEmail { get; set; }

        /// <summary>
        /// UserId des betroffenen Benutzers (optional, z.B. Belegbesitzer)
        /// </summary>
        public string? TargetUserId { get; set; }

        /// <summary>
        /// IP-Adresse des Clients (optional)
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// User-Agent (optional)
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// JSON-Details für flexible Datenspeicherung
        /// Beispiel: {"OldStatus": "Offen", "NewStatus": "InBearbeitung", "Amount": 45.99}
        /// </summary>
        public string? DetailsJson { get; set; }

        /// <summary>
        /// Beschreibung der Aktion (lesbar für Admin-UI)
        /// </summary>
        public string? Description { get; set; }
    }
}
