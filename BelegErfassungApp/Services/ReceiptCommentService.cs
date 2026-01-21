using BelegErfassungApp.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BelegErfassungApp.Services
{
    public class ReceiptCommentService : IReceiptCommentService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditLogService _auditLogService;
        private readonly IEmailService _emailService;
        private readonly ILogger<ReceiptCommentService> _logger;

        public ReceiptCommentService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IAuditLogService auditLogService,
            IEmailService emailService,
            ILogger<ReceiptCommentService> logger)
        {
            _context = context;
            _userManager = userManager;
            _auditLogService = auditLogService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<List<ReceiptCommentDto>> GetCommentsForReceiptAsync(int receiptId)
        {
            var comments = await _context.ReceiptComments
                .Include(c => c.User)
                .Where(c => c.ReceiptId == receiptId && !c.IsDeleted)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            // Organisiere Kommentare hierarchisch
            var commentDtos = comments
                .Where(c => c.ParentCommentId == null)
                .Select(c => MapToDto(c, comments))
                .ToList();

            return commentDtos;
        }

        private ReceiptCommentDto MapToDto(ReceiptComment comment, List<ReceiptComment> allComments)
        {
            var dto = new ReceiptCommentDto
            {
                Id = comment.Id,
                ReceiptId = comment.ReceiptId,
                UserId = comment.UserId,
                UserName = comment.User?.UserName ?? "Unbekannt",
                CommentText = comment.CommentText,
                CreatedAt = comment.CreatedAt,
                ParentCommentId = comment.ParentCommentId,
                IsAdminComment = comment.IsAdminComment,
                IsDeleted = comment.IsDeleted
            };

            // Lade Antworten
            dto.Replies = allComments
                .Where(c => c.ParentCommentId == comment.Id && !c.IsDeleted)
                .Select(c => MapToDto(c, allComments))
                .ToList();

            return dto;
        }

        public async Task<ReceiptCommentDto> AddCommentAsync(
            int receiptId,
            string userId,
            string commentText,
            int? parentCommentId = null)
        {
            var receipt = await _context.Receipts
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == receiptId);

            if (receipt == null)
            {
                throw new InvalidOperationException("Beleg nicht gefunden");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException("Benutzer nicht gefunden");
            }

            var isAdmin = await _userManager.IsInRoleAsync(user, "Administrator");

            var comment = new ReceiptComment
            {
                ReceiptId = receiptId,
                UserId = userId,
                CommentText = commentText,
                ParentCommentId = parentCommentId,
                IsAdminComment = isAdmin,
                CreatedAt = DateTime.UtcNow
            };

            _context.ReceiptComments.Add(comment);
            await _context.SaveChangesAsync();

            // Audit Log
            await _auditLogService.LogAsync(
                "ReceiptComment",
                "ADD",
                $"Kommentar zu Beleg #{receiptId} hinzugefügt: '{commentText.Substring(0, Math.Min(50, commentText.Length))}...'",
                userId
            );

            // E-Mail senden
            try
            {
                string recipientEmail;
                string recipientName;

                if (isAdmin)
                {
                    // Admin kommentiert → Benachrichtige Mitglied
                    recipientEmail = receipt.User.Email ?? string.Empty;
                    recipientName = receipt.User.UserName ?? "Mitglied";
                }
                else
                {
                    // Mitglied kommentiert → Benachrichtige Admin(s)
                    var admins = await _userManager.GetUsersInRoleAsync("Administrator");
                    var firstAdmin = admins.FirstOrDefault();

                    if (firstAdmin != null)
                    {
                        recipientEmail = firstAdmin.Email ?? string.Empty;
                        recipientName = "Administrator";
                    }
                    else
                    {
                        recipientEmail = string.Empty;
                        recipientName = string.Empty;
                    }
                }

                if (!string.IsNullOrEmpty(recipientEmail))
                {
                    await _emailService.SendCommentNotificationAsync(
                        recipientEmail,
                        recipientName,
                        receipt.FileName,
                        user.UserName ?? "Benutzer",
                        commentText,
                        isAdmin
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Senden der Kommentar-Benachrichtigung");
                // Fehler nicht werfen - Kommentar wurde gespeichert
            }

            return MapToDto(comment, new List<ReceiptComment> { comment });
        }

        public async Task<bool> DeleteCommentAsync(int commentId, string adminUserId)
        {
            var comment = await _context.ReceiptComments
                .FirstOrDefaultAsync(c => c.Id == commentId);

            if (comment == null)
            {
                return false;
            }

            // Soft Delete
            comment.IsDeleted = true;
            comment.DeletedAt = DateTime.UtcNow;
            comment.DeletedByUserId = adminUserId;

            await _context.SaveChangesAsync();

            // Audit Log
            await _auditLogService.LogAsync(
                "ReceiptComment",
                "DELETE",
                $"Kommentar #{commentId} zu Beleg #{comment.ReceiptId} gelöscht",
                adminUserId
            );

            return true;
        }

        public async Task<int> GetUnreadCommentCountForUserAsync(string userId)
        {
            // Zähle neue Kommentare an Belegen des Users
            var userReceipts = await _context.Receipts
                .Where(r => r.UserId == userId)
                .Select(r => r.Id)
                .ToListAsync();

            var count = await _context.ReceiptComments
                .Where(c => userReceipts.Contains(c.ReceiptId)
                    && c.UserId != userId
                    && !c.IsDeleted
                    && c.CreatedAt > DateTime.UtcNow.AddDays(-7)) // Letzte 7 Tage
                .CountAsync();

            return count;
        }
    }
}
