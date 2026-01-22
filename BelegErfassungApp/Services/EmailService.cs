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

        public async Task SendCommentNotificationAsync(
            string recipientEmail,
            string recipientName,
            string receiptFileName,
            string commenterName,
            string commentText,
            bool isAdminComment)
        {
            try
            {
                var subject = isAdminComment
                    ? $"💬 Neue Administrator-Antwort zu Beleg: {receiptFileName}"
                    : $"💬 Neuer Kommentar zu Beleg: {receiptFileName}";

                var body = $@"
            <h2>💬 Neuer Kommentar</h2>
            <p>Hallo {recipientName},</p>
            <p>{(isAdminComment ? "Ein Administrator" : commenterName)} hat einen Kommentar zu deinem Beleg hinterlassen:</p>
            <div style='background-color: #f5f5f5; padding: 15px; border-left: 4px solid #007bff; margin: 20px 0;'>
                <strong>{commenterName}:</strong><br/>
                {commentText}
            </div>
            <p><strong>Beleg:</strong> {receiptFileName}</p>
            <p><strong>Zeitstempel:</strong> {DateTime.Now:dd.MM.yyyy HH:mm:ss}</p>
            <p>Du kannst die Details in deinem Dashboard ansehen.</p>
            <p>Mit freundlichen Grüßen,<br/>Belegverwaltungs-System</p>
        ";

                await SendEmailAsync(recipientEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Fehler beim Versand der Kommentar-Benachrichtigung an {recipientEmail}");
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
        //private async Task SendEmailAsync(string recipientEmail, string subject, string body)
        //{
        //    var smtpHost = _configuration["Email:SmtpHost"];
        //    var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
        //    var senderEmail = _configuration["Email:SenderEmail"];
        //    var senderPassword = _configuration["Email:SenderPassword"];
        //    var senderName = _configuration["Email:SenderName"] ?? "Belegverwaltung";

        //    if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(senderEmail))
        //    {
        //        _logger.LogWarning("SMTP nicht konfiguriert. E-Mail wird nicht versendet.");
        //        return;
        //    }

        //    using (var client = new SmtpClient(smtpHost, smtpPort))
        //    {
        //        client.EnableSsl = true;
        //        client.Credentials = new NetworkCredential(senderEmail, senderPassword);

        //        var mailMessage = new MailMessage
        //        {
        //            From = new MailAddress(senderEmail, senderName),  // ✅ Jetzt sicher
        //            Subject = subject,
        //            Body = body,
        //            IsBodyHtml = true
        //        };

        //        mailMessage.To.Add(new MailAddress(recipientEmail));

        //        try
        //        {
        //            await client.SendMailAsync(mailMessage);
        //            _logger.LogInformation($"E-Mail erfolgreich versendet an {recipientEmail}");
        //        }
        //        finally
        //        {
        //            mailMessage.Dispose();
        //        }
        //    }
        //}

        //private async Task SendEmailAsync(string recipientEmail, string subject, string body)
        //{
        //    var smtpHost = _configuration["Email:SmtpHost"];
        //    var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
        //    var senderEmail = _configuration["Email:SenderEmail"];
        //    var senderPassword = _configuration["Email:SenderPassword"];
        //    var senderName = _configuration["Email:SenderName"] ?? "Belegverwaltung";

        //    if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(senderEmail))
        //    {
        //        _logger.LogWarning("SMTP nicht konfiguriert. E-Mail wird nicht versendet.");
        //        return;
        //    }

        //    _logger.LogInformation($"📧 Versuche E-Mail zu versenden an {recipientEmail}");

        //    using (var client = new SmtpClient(smtpHost, smtpPort))
        //    {
        //        // SSL nur aktivieren, wenn NICHT Port 25 (für Relay-Container)
        //        client.EnableSsl = (smtpPort != 25);

        //        // Bei Port 25 (Relay) keine Credentials nötig
        //        if (smtpPort == 25)
        //        {
        //            client.UseDefaultCredentials = true;
        //        }
        //        else
        //        {
        //            client.UseDefaultCredentials = false;
        //            client.Credentials = new NetworkCredential(senderEmail, senderPassword);
        //        }

        //        client.DeliveryMethod = SmtpDeliveryMethod.Network;
        //        client.Timeout = 30000;

        //        var mailMessage = new MailMessage
        //        {
        //            From = new MailAddress(senderEmail, senderName),
        //            Subject = subject,
        //            Body = body,
        //            IsBodyHtml = true
        //        };

        //        mailMessage.To.Add(new MailAddress(recipientEmail));

        //        try
        //        {
        //            await client.SendMailAsync(mailMessage);
        //            _logger.LogInformation($"✅ E-Mail erfolgreich versendet an {recipientEmail}");
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, $"❌ SMTP-Fehler bei {recipientEmail}: {ex.Message}");
        //            throw;
        //        }
        //        finally
        //        {
        //            mailMessage.Dispose();
        //        }
        //    }
        //}

        private async Task SendEmailAsync(string recipientEmail, string subject, string body)
        {
            _logger.LogInformation("=== E-Mail-Versand Start ===");
            _logger.LogDebug("Recipient: {Recipient}, Subject: {Subject}", recipientEmail, subject);

            // 1. Konfiguration laden
            var smtpHost = _configuration["Email:SmtpHost"];
            var smtpPortStr = _configuration["Email:SmtpPort"] ?? "587";
            var senderEmail = _configuration["Email:SenderEmail"];
            var senderPassword = _configuration["Email:SenderPassword"];
            var senderName = _configuration["Email:SenderName"] ?? "Belegverwaltung";

            // 2. Konfiguration validieren & loggen
            _logger.LogInformation("📋 SMTP-Konfiguration geladen: Host={SmtpHost}, Port={SmtpPort}, Sender={Sender}",
                smtpHost ?? "<<NULL>>", smtpPortStr, senderEmail ?? "<<NULL>>");

            if (string.IsNullOrWhiteSpace(smtpHost))
            {
                _logger.LogError("❌ SMTP Host nicht konfiguriert! Bitte Email:SmtpHost in appsettings.json setzen");
                return;
            }

            if (string.IsNullOrWhiteSpace(senderEmail))
            {
                _logger.LogError("❌ Sender Email nicht konfiguriert! Bitte Email:SenderEmail in appsettings.json setzen");
                return;
            }

            if (!int.TryParse(smtpPortStr, out var smtpPort))
            {
                _logger.LogError("❌ SmtpPort ungültig: '{Port}' ist keine ganze Zahl", smtpPortStr);
                return;
            }

            if (smtpPort < 1 || smtpPort > 65535)
            {
                _logger.LogError("❌ SmtpPort außerhalb gültigen Bereichs: {Port} (1-65535)", smtpPort);
                return;
            }

            // 3. Empfänger validieren
            try
            {
                var _ = new MailAddress(recipientEmail);
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "❌ Empfänger-Email ungültig: {Recipient}", recipientEmail);
                return;
            }

            // 4. SmtpClient erstellen & Loggen
            _logger.LogInformation("📧 Erstelle SMTP-Verbindung zu {Host}:{Port}, SSL={EnableSSL}",
                smtpHost, smtpPort, smtpPort != 25);

            using (var client = new SmtpClient(smtpHost, smtpPort))
            {
                try
                {
                    // 5. Verbindungsparameter konfigurieren
                    client.EnableSsl = (smtpPort != 25);
                    client.Timeout = 30000;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;

                    if (smtpPort == 25)
                    {
                        _logger.LogDebug("⚙️ Port 25 erkannt (Relay-Modus) → UseDefaultCredentials = true");
                        client.UseDefaultCredentials = true;
                    }
                    else
                    {
                        _logger.LogDebug("⚙️ Port {Port} → Verwende Credentials für User: {User}", smtpPort, senderEmail);
                        client.UseDefaultCredentials = false;
                        client.Credentials = new NetworkCredential(senderEmail, senderPassword);
                    }

                    // 6. MailMessage erstellen
                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(senderEmail, senderName),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(new MailAddress(recipientEmail));

                    _logger.LogInformation("📤 Versende E-Mail von {From} an {To}...",
                        mailMessage.From.Address, recipientEmail);

                    // 7. E-Mail versenden
                    await client.SendMailAsync(mailMessage);

                    _logger.LogInformation("✅ E-Mail erfolgreich versendet an {Recipient}", recipientEmail);
                    mailMessage.Dispose();
                }
                catch (SmtpException smtpEx)
                {
                    _logger.LogError(smtpEx,
                        "❌ SMTP-Fehler (Code: {StatusCode}): {Message}\n   InnerException: {InnerMessage}\n   Prüfe: Host={Host}, Port={Port}, Authentifizierung, Firewall",
                        smtpEx.StatusCode, smtpEx.Message, smtpEx.InnerException?.Message ?? "keine", smtpHost, smtpPort);
                    throw;
                }
                catch (IOException ioEx)
                {
                    _logger.LogError(ioEx,
                        "❌ Verbindungsfehler: {Message}\n   InnerException: {InnerMessage}\n   Prüfe: Host erreichbar? Firewall? SSL/TLS-Problem?",
                        ioEx.Message, ioEx.InnerException?.Message ?? "keine");
                    throw;
                }
                catch (InvalidOperationException invEx)
                {
                    _logger.LogError(invEx,
                        "❌ Ungültige Operation: {Message}\n   Typischerweise: SMTP-Client bereits versendet oder Credentials-Problem",
                        invEx.Message);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "❌ Unerwarteter Fehler beim E-Mail-Versand: {Message}\n   Typ: {ExceptionType}",
                        ex.Message, ex.GetType().FullName);
                    throw;
                }
            }
        }




    }
}
