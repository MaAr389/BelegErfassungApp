using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BelegErfassungApp.Data
{
    public class MemberApplication
    {
        [Key]
        public int Id { get; set; }

        // ── Metadaten ─────────────────────────────────────────
        public string UploadedByUserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UploadedByUserId))]
        public ApplicationUser? UploadedByUser { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public MemberApplicationStatus Status { get; set; } = MemberApplicationStatus.Eingegangen;

        public string? ProcessedByUserId { get; set; }

        [ForeignKey(nameof(ProcessedByUserId))]
        public ApplicationUser? ProcessedByUser { get; set; }

        public DateTime? ProcessedAt { get; set; }

        public string? ProcessingNote { get; set; }  // z.B. "In WisoMeinVerein angelegt unter ID 12345"

        // ── Datei ─────────────────────────────────────────────
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;

        // ── OCR-Ergebnis: Personendaten ───────────────────────
        public string? Nachname { get; set; }
        public string? Vorname { get; set; }
        public string? Geburtsdatum { get; set; }
        public string? BerufTaetigkeit { get; set; }
        public string? Strasse { get; set; }
        public string? PLZ { get; set; }
        public string? Wohnort { get; set; }
        public string? Telefon { get; set; }
        public string? Email { get; set; }
        public string? Antragsdatum { get; set; }
        public bool UnterschriftVorhanden { get; set; }

        // ── OCR-Ergebnis: SEPA-Einzugsermächtigung ────────────
        public string? Kontoinhaber { get; set; }
        public string? Geldinstitut { get; set; }
        public string? BIC { get; set; }
        public string? IBAN { get; set; }
        public string? SEPADatum { get; set; }
        public bool SEPAUnterschriftVorhanden { get; set; }

        // ── OCR-Rohtext ───────────────────────────────────────
        public string? OcrRawText { get; set; }
        public bool OcrProcessed { get; set; }
    }

    public enum MemberApplicationStatus
    {
        Eingegangen,
        InBearbeitung,
        Angelegt,
        Abgelehnt
    }
}
