using System.Threading.Tasks;

namespace BelegErfassungApp.Services
{
    public interface IEmailService
    {
        /// <summary>
        /// Sendet eine E-Mail-Benachrichtigung bei Status-Änderung
        /// </summary>
        Task SendStatusChangeNotificationAsync(
            string recipientEmail,
            string userName,
            string receiptFileName,
            string oldStatus,
            string newStatus);

        /// <summary>
        /// Sendet eine E-Mail an Admin bei neuem Beleg
        /// </summary>
        Task SendNewReceiptNotificationAsync(
            string adminEmail,
            string memberName,
            string receiptFileName,
            decimal amount);

        /// <summary>
        /// Sendet eine Test-E-Mail (für Debugging)
        /// </summary>
        /// 
        Task SendCommentNotificationAsync(
            string recipientEmail,
            string recipientName,
            string receiptFileName,
            string commenterName,
            string commentText,
            bool isAdminComment);

        Task SendTestEmailAsync(string recipientEmail);
    }
}
