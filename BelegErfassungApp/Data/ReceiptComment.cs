using System.ComponentModel.DataAnnotations;

namespace BelegErfassungApp.Data
{
    public class ReceiptComment
    {
        public int Id { get; set; }

        [Required]
        public int ReceiptId { get; set; }
        public Receipt Receipt { get; set; } = null!;

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        [Required]
        [MaxLength(2000)]
        public string CommentText { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Für Antworten auf andere Kommentare
        public int? ParentCommentId { get; set; }
        public ReceiptComment? ParentComment { get; set; }

        // Für Admin-Tracking
        public bool IsAdminComment { get; set; }

        // Soft Delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedByUserId { get; set; }
    }
}
