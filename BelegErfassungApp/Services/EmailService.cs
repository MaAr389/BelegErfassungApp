using BelegErfassungApp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace BelegErfassungApp.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendStatusChangeNotificationAsync(
            string recipientEmail,
            string userName,
            string receiptFileName,
            string oldStatus,
            string newStatus)
        {
            try
            {
                var subject = $"Beleg-Status aktualisiert: {receiptFileName}";

                var body = $@"
                    <h2>📋 Beleg-Status Benachrichtigung</h2>
                    <p>Hallo {userName},</p>
                    <p>Der Status deines Belegs wurde aktualisiert:</p>
                    <ul>
                        <li><strong>Datei:</strong> {receiptFileName}</li>
                        <li><strong>Alter Status:</strong> <span style='color: orange;'>{oldStatus}</span></li>
                        <li><strong>Neuer Status:</strong> <span style='color: green;'>{newStatus}</span></li>
                        <li><strong>Zeitstempel:</strong> {DateTime.Now:dd.MM.yyyy HH:mm:ss}</li>
                    </ul>
                    <p>Du kannst die Details in deinem Dashboard ansehen.</p>
                    <p>Mit freundlichen Grüßen,<br/>Belegverwaltungs-System</p>
                ";

                await SendEmailAsync(recipientEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Fehler beim Versand der Status-Benachrichtigung an {recipientEmail}");
                throw;
            }
        }

        public async Task SendNewReceiptNotificationAsync(
            string adminEmail,
            string memberName,
            string receiptFileName,
            decimal amount)
        {
            try
            {
                var subject = $"🔔 Neuer Beleg eingereicht: {receiptFileName}";

                var body = $@"
                    <h2>📤 Neuer Beleg eingereicht</h2>
                    <p>Ein neuer Beleg wurde hochgeladen:</p>
                    <ul>
                        <li><strong>Mitglied:</strong> {memberName}</li>
                        <li><strong>Datei:</strong> {receiptFileName}</li>
                        <li><strong>Betrag:</strong> €{amount:F2}</li>
                        <li><strong>Zeitstempel:</strong> {DateTime.Now:dd.MM.yyyy HH:mm:ss}</li>
                    </ul>
                    <p><a href='https://belegverwaltung.de/admin/allreceipts'>👉 Zum Admin-Dashboard</a></p>
                    <p>Mit freundlichen Grüßen,<br/>Belegverwaltungs-System</p>
                ";

                await SendEmailAsync(adminEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Fehler beim Versand der Admin-Benachrichtigung an {adminEmail}");
                throw;
            }
        }

        public async Task SendTestEmailAsync(string recipientEmail)
        {
            try
            {
                var subject = "✅ Test-E-Mail";
                var body = "<p>Dies ist eine Test-E-Mail. Wenn du diese erhältst, funktioniert der E-Mail-Service!</p>";

                await SendEmailAsync(recipientEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Fehler beim Versand der Test-E-Mail an {recipientEmail}");
                throw;
            }
        }

        // Private Hilfsmethode
        private async Task SendEmailAsync(string recipientEmail, string subject, string body)
        {
            var smtpHost = _configuration["Email:SmtpHost"];
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var senderEmail = _configuration["Email:SenderEmail"];
            var senderPassword = _configuration["Email:SenderPassword"];
            var senderName = _configuration["Email:SenderName"] ?? "Belegverwaltung";

            if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(senderEmail))
            {
                _logger.LogWarning("SMTP nicht konfiguriert. E-Mail wird nicht versendet.");
                return;
            }

            using (var client = new SmtpClient(smtpHost, smtpPort))
            {
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(senderEmail, senderPassword);

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),  // ✅ Jetzt sicher
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(new MailAddress(recipientEmail));

                try
                {
                    await client.SendMailAsync(mailMessage);
                    _logger.LogInformation($"E-Mail erfolgreich versendet an {recipientEmail}");
                }
                finally
                {
                    mailMessage.Dispose();
                }
            }
        }

    }
}
