using System.ComponentModel.DataAnnotations;
namespace BelegErfassungApp.Data
{
    public class Receipt
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        // Upload-Informationen
        public DateTime UploadDate { get; set; } = DateTime.Now;

        [Required]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public string FilePath { get; set; } = string.Empty;

        // Manuelle Eingaben
        [Required]
        public DateTime ReceiptDate { get; set; }

        [Required]
        [Range(0.01, 999999.99)]
        public decimal ManualPrice { get; set; }

        // OCR-Ergebnisse
        public decimal? OcrGrossAmount { get; set; }
        public decimal? OcrNetAmount { get; set; }
        public decimal? OcrVatAmount { get; set; }
        public DateTime? OcrReceiptDate { get; set; }
        public double? OcrConfidence { get; set; }

        // Verwaltung
        public ReceiptStatus Status { get; set; } = ReceiptStatus.Offen;
        public DateTime? StatusChangedDate { get; set; }
    }

    public enum ReceiptStatus
    {
        Offen = 0,
        InBearbeitung = 1,
        Abgeschlossen = 2
    }
}
