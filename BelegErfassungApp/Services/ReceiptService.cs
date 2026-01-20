using BelegErfassungApp.Data;
using BelegErfassungApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BelegErfassungApp.Services
{
    public interface IReceiptService
    {

        Task<Receipt> CreateReceiptAsync(string userId, Stream fileStream,
            string fileName, string contentType, DateTime receiptDate, decimal manualPrice);
        Task<List<Receipt>> GetUserReceiptsAsync(string userId);
        Task<List<Receipt>> GetAllReceiptsAsync();
        Task<Receipt?> GetReceiptByIdAsync(int id);
        Task UpdateStatusAsync(int receiptId, ReceiptStatus newStatus, string adminUserId); // Auditlog "string adminUserId" ergänzt
        Task<string> GetReceiptFilePathAsync(int receiptId);
        Task DeleteReceiptAsync(int receiptId, string adminUserId); // Auditlog "string adminUserId" ergänzt

    }

    public class ReceiptService : IReceiptService
    {
        private readonly ApplicationDbContext _context;
        private readonly IOcrService _ocrService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ReceiptService> _logger;
        private readonly string _uploadPath;

        private readonly IAuditLogService _auditLogService; // NEU AUDIT
        private readonly IEmailService _emailService;

        public ReceiptService(
            ApplicationDbContext context,
            IOcrService ocrService,
            IWebHostEnvironment environment,
            IConfiguration configuration,
            ILogger<ReceiptService> logger,
            IAuditLogService auditLogService,
            IEmailService emailService)
        {
            _context = context;
            _ocrService = ocrService;
            _environment = environment;
            _logger = logger;
            _auditLogService = auditLogService;
            _emailService = emailService;
            _uploadPath = configuration["FileUpload:UploadPath"] ?? "wwwroot/uploads";

            // Sicherstellen, dass Upload-Verzeichnis existiert
            var fullPath = Path.Combine(_environment.ContentRootPath, _uploadPath);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                _logger.LogInformation($"Upload-Verzeichnis erstellt: {fullPath}");
            }
        }




        public async Task<Receipt> CreateReceiptAsync(
            string userId,
            Stream fileStream,
            string fileName,
            string contentType,
            DateTime receiptDate,
            decimal manualPrice)
        {
            try
            {
                _logger.LogInformation($"CreateReceiptAsync gestartet für User {userId}, Datei: {fileName}");

                // 1. Eindeutigen Dateinamen generieren
                var fileExtension = Path.GetExtension(fileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var relativePath = Path.Combine(_uploadPath, uniqueFileName);
                var fullPath = Path.Combine(_environment.ContentRootPath, relativePath);

                _logger.LogInformation($"Datei wird gespeichert unter: {fullPath}");

                // 2. Datei lokal speichern
                using (var fileStreamLocal = new FileStream(fullPath, FileMode.Create))
                {
                    await fileStream.CopyToAsync(fileStreamLocal);
                }
                _logger.LogInformation($"Datei erfolgreich gespeichert: {uniqueFileName}");

                // 3. Stream zurücksetzen für OCR
                if (fileStream.CanSeek)
                {
                    fileStream.Position = 0;
                }
                else
                {
                    // Stream neu öffnen falls nicht seekable
                    using var newStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
                    var ocrResult = await _ocrService.AnalyzeReceiptAsync(newStream);

                    _logger.LogInformation($"OCR abgeschlossen. Konfidenz: {ocrResult.Confidence:P}");

                    // 4. Receipt in Datenbank speichern
                    var receipt = new Receipt
                    {
                        UserId = userId,
                        UploadDate = DateTime.UtcNow,
                        FileName = fileName,
                        FilePath = relativePath,
                        ReceiptDate = receiptDate,
                        ManualPrice = manualPrice,

                        // OCR-Ergebnisse
                        OcrGrossAmount = ocrResult.GrossAmount,
                        OcrNetAmount = ocrResult.NetAmount,
                        OcrVatAmount = ocrResult.VatAmount,
                        OcrReceiptDate = ocrResult.ReceiptDate,
                        OcrConfidence = ocrResult.Confidence,

                        Status = ReceiptStatus.Offen
                    };

                    _context.Receipts.Add(receipt);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Receipt {receipt.Id} erfolgreich in DB gespeichert");

                    // AUDIT-LOG: Beleg hochgeladen (NON-SEEKABLE Block)
                    await _auditLogService.LogAsync(
                        action: "ReceiptCreated",
                        entityType: "Receipt",
                        entityId: receipt.Id.ToString(),
                        actorUserId: userId,
                        targetUserId: userId,
                        detailsJson: $"{{\"FileName\": \"{fileName}\", \"Amount\": {manualPrice}, \"OcrConfidence\": {ocrResult.Confidence:P}}}",
                        description: $"Beleg hochgeladen: {fileName} ({manualPrice}€)"
                    );

                    return receipt;
                }

                // Falls Stream seekable ist
                var ocrResultSeekable = await _ocrService.AnalyzeReceiptAsync(fileStream);

                _logger.LogInformation($"OCR abgeschlossen. Konfidenz: {ocrResultSeekable.Confidence:P}");

                // 4. Receipt in Datenbank speichern
                var receiptSeekable = new Receipt
                {
                    UserId = userId,
                    UploadDate = DateTime.UtcNow,
                    FileName = fileName,
                    FilePath = relativePath,
                    ReceiptDate = receiptDate,
                    ManualPrice = manualPrice,

                    // OCR-Ergebnisse
                    OcrGrossAmount = ocrResultSeekable.GrossAmount,
                    OcrNetAmount = ocrResultSeekable.NetAmount,
                    OcrVatAmount = ocrResultSeekable.VatAmount,
                    OcrReceiptDate = ocrResultSeekable.ReceiptDate,
                    OcrConfidence = ocrResultSeekable.Confidence,

                    Status = ReceiptStatus.Offen
                };

                _context.Receipts.Add(receiptSeekable);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Receipt {receiptSeekable.Id} erfolgreich in DB gespeichert");

                //AUDIT-LOG: Beleg hochgeladen (SEEKABLE Block)
                await _auditLogService.LogAsync(
                    action: "ReceiptCreated",
                    entityType: "Receipt",
                    entityId: receiptSeekable.Id.ToString(),
                    actorUserId: userId,
                    targetUserId: userId,
                    detailsJson: $"{{\"FileName\": \"{fileName}\", \"Amount\": {manualPrice}, \"OcrConfidence\": {ocrResultSeekable.Confidence:P}}}",
                    description: $"Beleg hochgeladen: {fileName} ({manualPrice}€)"
                );

                return receiptSeekable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Fehler beim Erstellen des Receipts für User {userId}");
                throw;
            }
        }

        public async Task<List<Receipt>> GetUserReceiptsAsync(string userId)
        {
            return await _context.Receipts
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.UploadDate)
                .ToListAsync();
        }

        public async Task<List<Receipt>> GetAllReceiptsAsync()
        {
            return await _context.Receipts
                .Include(r => r.User)
                .OrderByDescending(r => r.UploadDate)
                .ToListAsync();
        }

        public async Task<Receipt?> GetReceiptByIdAsync(int id)
        {
            return await _context.Receipts
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        // Original bevor Implementation Audit Log
        //public async Task UpdateStatusAsync(int receiptId, ReceiptStatus newStatus)
        //{
        //    var receipt = await _context.Receipts.FindAsync(receiptId);
        //    if (receipt != null)
        //    {
        //        receipt.Status = newStatus;
        //        receipt.StatusChangedDate = DateTime.UtcNow;
        //        await _context.SaveChangesAsync();

        //        _logger.LogInformation($"Receipt {receiptId} Status auf {newStatus} gesetzt");
        //    }
        //    else
        //    {
        //        _logger.LogWarning($"Receipt {receiptId} nicht gefunden für Status-Update");
        //    }
        //}

        //public async Task UpdateStatusAsync(int receiptId, ReceiptStatus newStatus, string adminUserId)
        //{
        //    try
        //    {
        //        // Beleg laden mit User-Daten
        //        var receipt = await _context.Receipts
        //            .Include(r => r.User)
        //            .FirstOrDefaultAsync(r => r.Id == receiptId);

        //        if (receipt == null)
        //        {
        //            _logger.LogWarning($"Receipt {receiptId} nicht gefunden für Status-Update");
        //            throw new InvalidOperationException($"Beleg {receiptId} nicht gefunden");
        //        }

        //        // Alten Status speichern für Audit-Log
        //        var oldStatus = receipt.Status;

        //        // Status aktualisieren
        //        receipt.Status = newStatus;
        //        receipt.StatusChangedDate = DateTime.UtcNow;
        //        await _context.SaveChangesAsync();

        //        _logger.LogInformation($"Receipt {receiptId} Status von {oldStatus} auf {newStatus} durch {adminUserId} geändert");

        //        // 🔍 AUDIT-LOG: Status geändert
        //        await _auditLogService.LogAsync(
        //            action: "StatusChanged",
        //            entityType: "Receipt",
        //            entityId: receiptId.ToString(),
        //            actorUserId: adminUserId,
        //            targetUserId: receipt.UserId,
        //            detailsJson: $"{{\"OldStatus\": \"{oldStatus}\", \"NewStatus\": \"{newStatus}\"}}",
        //            description: $"Status geändert: {oldStatus} → {newStatus} (Admin: {receipt.User?.Email})"
        //        );

        //        // 📧 EMAIL-BENACHRICHTIGUNG: Dem Benutzer Bescheid geben
        //        if (!string.IsNullOrWhiteSpace(receipt.User?.Email))
        //        {
        //            try
        //            {
        //                await _emailService.SendStatusChangeNotificationAsync(
        //                    recipientEmail: receipt.User.Email,
        //                    userName: receipt.User.UserName ?? "Benutzer",
        //                    receiptFileName: receipt.FileName,
        //                    oldStatus: oldStatus.ToString(),
        //                    newStatus: newStatus.ToString()
        //                );
        //                _logger.LogInformation($"Status-Benachrichtigung versendet an {receipt.User.Email}");
        //            }
        //            catch (Exception ex)
        //            {
        //                _logger.LogError(ex, $"Fehler beim Versand der E-Mail-Benachrichtigung");
        //                // Fehler nicht werfen - Beleg-Update soll trotzdem erfolgreich sein
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, $"Fehler beim Status-Update für Receipt {receiptId}");
        //        throw;
        //    }
        //}

        public async Task UpdateStatusAsync(int receiptId, ReceiptStatus newStatus, string adminUserId)
        {
            try
            {
                var receipt = await _context.Receipts.FindAsync(receiptId);

                if (receipt == null)
                {
                    throw new InvalidOperationException($"Beleg {receiptId} nicht gefunden");
                }

                var oldStatus = receipt.Status;
                receipt.Status = newStatus;
                receipt.StatusChangedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Receipt {receiptId} Status aktualisiert: {oldStatus} → {newStatus}");

                // 🔍 AUDIT-LOG
                try
                {
                    await _auditLogService.LogAsync(
                        action: "StatusChanged",
                        entityType: "Receipt",
                        entityId: receiptId.ToString(),
                        actorUserId: adminUserId,
                        targetUserId: receipt.UserId,
                        detailsJson: $"{{\"OldStatus\": \"{oldStatus}\", \"NewStatus\": \"{newStatus}\"}}",
                        description: $"Status geändert: {oldStatus} → {newStatus}"
                    );
                }
                catch (Exception auditEx)
                {
                    _logger.LogError(auditEx, "Audit-Log Fehler");
                }

                // 📧 EMAIL-BENACHRICHTIGUNG
                if (_emailService != null)  // ← Prüfe ob Service existiert!
                {
                    try
                    {
                        var user = await _context.Users.FindAsync(receipt.UserId);

                        if (user != null && !string.IsNullOrWhiteSpace(user.Email))
                        {
                            _logger.LogInformation($"📧 Versuche E-Mail zu versenden an {user.Email}");

                            await _emailService.SendStatusChangeNotificationAsync(
                                recipientEmail: user.Email,
                                userName: user.Email ?? "Benutzer",
                                receiptFileName: receipt.FileName,
                                oldStatus: oldStatus.ToString(),
                                newStatus: newStatus.ToString()
                            );

                            _logger.LogInformation($"✅ E-Mail versendet an {user.Email}");
                        }
                        else
                        {
                            _logger.LogWarning("⚠️ User oder E-Mail nicht gefunden");
                        }
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "❌ E-Mail-Versand fehlgeschlagen");
                        // NICHT werfen - Status ist bereits aktualisiert
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ EmailService ist NULL - nicht injiziert?");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Fehler beim Status-Update für Receipt {receiptId}");
                throw;
            }
        }







        //public async Task UpdateStatusAsync(int receiptId, ReceiptStatus newStatus, string adminUserId)
        //{
        //    try
        //    {
        //        // Beleg laden mit User-Daten
        //        var receipt = await _context.Receipts
        //            .Include(r => r.User)
        //            .FirstOrDefaultAsync(r => r.Id == receiptId);

        //        if (receipt == null)
        //        {
        //            _logger.LogWarning($"Receipt {receiptId} nicht gefunden für Status-Update");
        //            throw new InvalidOperationException($"Beleg {receiptId} nicht gefunden");
        //        }

        //        // Alten Status speichern für Audit-Log
        //        var oldStatus = receipt.Status;

        //        // Status aktualisieren
        //        receipt.Status = newStatus;
        //        receipt.StatusChangedDate = DateTime.UtcNow;
        //        await _context.SaveChangesAsync();

        //        _logger.LogInformation($"Receipt {receiptId} Status von {oldStatus} auf {newStatus} durch {adminUserId} geändert");

        //        // 🔍 AUDIT-LOG: Status geändert
        //        await _auditLogService.LogAsync(
        //            action: "StatusChanged",
        //            entityType: "Receipt",
        //            entityId: receiptId.ToString(),
        //            actorUserId: adminUserId,
        //            targetUserId: receipt.UserId,
        //            detailsJson: $"{{\"OldStatus\": \"{oldStatus}\", \"NewStatus\": \"{newStatus}\"}}",
        //            description: $"Status geändert: {oldStatus} → {newStatus} (Admin: {receipt.User?.Email})"
        //        );
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, $"Fehler beim Status-Update für Receipt {receiptId}");
        //        throw;
        //    }
        //}


        public async Task<string> GetReceiptFilePathAsync(int receiptId)
        {
            var receipt = await _context.Receipts.FindAsync(receiptId);
            if (receipt == null)
            {
                throw new FileNotFoundException($"Receipt {receiptId} nicht gefunden");
            }

            var fullPath = Path.Combine(_environment.ContentRootPath, receipt.FilePath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Datei nicht gefunden: {receipt.FileName}");
            }

            return fullPath;
        }

        // Original bevor Implementation Audit Log
        //public async Task DeleteReceiptAsync(int receiptId)
        //{
        //    try
        //    {
        //        // Beleg aus der Datenbank laden
        //        var receipt = await _context.Receipts
        //            .Include(r => r.User)  // Falls du auf User-Daten zugreifen musst
        //            .FirstOrDefaultAsync(r => r.Id == receiptId);

        //        if (receipt == null)
        //        {
        //            throw new InvalidOperationException($"Beleg mit ID {receiptId} nicht gefunden");
        //        }

        //        // Optional: Datei vom Dateisystem löschen
        //        if (!string.IsNullOrEmpty(receipt.FilePath))
        //        {
        //            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), receipt.FilePath.TrimStart('/'));
        //            if (File.Exists(fullPath))
        //            {
        //                File.Delete(fullPath);
        //            }
        //        }

        //        // Beleg aus der Datenbank entfernen
        //        _context.Receipts.Remove(receipt);

        //        // Änderungen speichern
        //        await _context.SaveChangesAsync();
        //    }
        //    catch (Exception ex)
        //    {
        //        // Fehler loggen
        //        Console.WriteLine($"Fehler beim Löschen des Belegs: {ex.Message}");
        //        throw;
        //    }
        //}
        public async Task DeleteReceiptAsync(int receiptId, string adminUserId)
        {
            try
            {
                var receipt = await _context.Receipts
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.Id == receiptId);

                if (receipt == null)
                {
                    throw new InvalidOperationException($"Beleg mit ID {receiptId} nicht gefunden");
                }

                // Metadaten vor dem Löschen speichern
                var fileName = receipt.FileName;
                var userId = receipt.UserId;
                var amount = receipt.ManualPrice;

                // Optional: Datei vom Dateisystem löschen
                if (!string.IsNullOrEmpty(receipt.FilePath))
                {
                    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), receipt.FilePath.TrimStart('/'));
                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                        _logger.LogInformation($"Datei gelöscht: {fileName}");
                    }
                }

                // Beleg aus der Datenbank entfernen
                _context.Receipts.Remove(receipt);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Receipt {receiptId} gelöscht durch {adminUserId}");

                // 🔍 AUDIT-LOG: Beleg gelöscht
                await _auditLogService.LogAsync(
                    action: "ReceiptDeleted",
                    entityType: "Receipt",
                    entityId: receiptId.ToString(),
                    actorUserId: adminUserId,
                    targetUserId: userId,
                    detailsJson: $"{{\"FileName\": \"{fileName}\", \"Amount\": {amount}}}",
                    description: $"Beleg gelöscht: {fileName}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Fehler beim Löschen des Belegs {receiptId}");
                throw;
            }
        }



        //public async Task DeleteReceiptAsync(int receiptId)
        //{
        //    var receipt = await _context.Receipts.FindAsync(receiptId);
        //    if (receipt != null)
        //    {
        //        // Datei löschen
        //        var fullPath = Path.Combine(_environment.ContentRootPath, receipt.FilePath);
        //        if (File.Exists(fullPath))
        //        {
        //            File.Delete(fullPath);
        //            _logger.LogInformation($"Datei gelöscht: {receipt.FileName}");
        //        }

        //        // Datenbank-Eintrag löschen
        //        _context.Receipts.Remove(receipt);
        //        await _context.SaveChangesAsync();

        //        _logger.LogInformation($"Receipt {receiptId} gelöscht");
        //    }
        //}
    }
}
