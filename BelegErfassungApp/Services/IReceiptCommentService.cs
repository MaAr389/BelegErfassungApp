using BelegErfassungApp.Data;

namespace BelegErfassungApp.Services
{
    public interface IReceiptCommentService
    {
        Task<List<ReceiptCommentDto>> GetCommentsForReceiptAsync(int receiptId);
        Task<ReceiptCommentDto> AddCommentAsync(int receiptId, string userId, string commentText, int? parentCommentId = null);
        Task<bool> DeleteCommentAsync(int commentId, string adminUserId);
        Task<int> GetUnreadCommentCountForUserAsync(string userId);
    }

    public class ReceiptCommentDto
    {
        public int Id { get; set; }
        public int ReceiptId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string CommentText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int? ParentCommentId { get; set; }
        public bool IsAdminComment { get; set; }
        public bool IsDeleted { get; set; }
        public List<ReceiptCommentDto> Replies { get; set; } = new();
    }
}
