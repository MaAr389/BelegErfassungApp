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
        Task UpdateReceiptAsync(Receipt receipt);
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

        public async Task UpdateStatusAsync(int receiptId, ReceiptStatus newStatus, string adminUserId)
        {
            try
            {
                // 1. Receipt UND User gleichzeitig laden (mit Include)
                var receipt = await _context.Receipts
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.Id == receiptId);

                if (receipt == null)
                {
                    throw new InvalidOperationException($"Beleg {receiptId} nicht gefunden");
                }

                // User-Daten zwischenspeichern VOR SaveChanges
                var userEmail = receipt.User?.Email;
                var userName = receipt.User?.UserName ?? receipt.User?.Email ?? "Benutzer";
                var userId = receipt.UserId;

                // 2. Status aktualisieren
                var oldStatus = receipt.Status;
                receipt.Status = newStatus;
                receipt.StatusChangedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Receipt {receiptId} Status aktualisiert: {oldStatus} → {newStatus}");

                // 3. AUDIT-LOG (nach SaveChanges, aber mit zwischengespeicherten Daten)
                try
                {
                    await _auditLogService.LogAsync(
                        action: "StatusChanged",
                        entityType: "Receipt",
                        entityId: receiptId.ToString(),
                        actorUserId: adminUserId,
                        targetUserId: userId,
                        detailsJson: $"{{\"OldStatus\": \"{oldStatus}\", \"NewStatus\": \"{newStatus}\"}}",
                        description: $"Status geändert: {oldStatus} → {newStatus}"
                    );
                }
                catch (Exception auditEx)
                {
                    _logger.LogError(auditEx, "Audit-Log Fehler");
                }

                // 4. EMAIL-BENACHRICHTIGUNG (mit zwischengespeicherten Daten)
                if (_emailService != null && !string.IsNullOrWhiteSpace(userEmail))
                {
                    try
                    {
                        _logger.LogInformation($"📧 Versuche E-Mail zu versenden an {userEmail}");

                        await _emailService.SendStatusChangeNotificationAsync(
                            recipientEmail: userEmail,
                            userName: userName,
                            receiptFileName: receipt.FileName,
                            oldStatus: oldStatus.ToString(),
                            newStatus: newStatus.ToString()
                        );

                        _logger.LogInformation($"✅ E-Mail versendet an {userEmail}");
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "❌ E-Mail-Versand fehlgeschlagen");
                        // NICHT werfen - Status ist bereits aktualisiert
                    }
                }
                else if (_emailService == null)
                {
                    _logger.LogWarning("⚠️ EmailService ist NULL - nicht injiziert?");
                }
                else
                {
                    _logger.LogWarning("⚠️ User-E-Mail nicht gefunden");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Fehler beim Status-Update für Receipt {receiptId}");
                throw;
            }
        }
        public async Task UpdateReceiptAsync(Receipt receipt)
        {
            _context.Receipts.Update(receipt);
            await _context.SaveChangesAsync();
        }

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

                // 🔥 NEU: Zuerst alle Kommentare zu diesem Beleg löschen
                var comments = await _context.ReceiptComments
                    .Where(c => c.ReceiptId == receiptId)
                    .ToListAsync();

                if (comments.Any())
                {
                    _context.ReceiptComments.RemoveRange(comments);
                    _logger.LogInformation($"{comments.Count} Kommentare zu Receipt {receiptId} gelöscht");
                }

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
                    detailsJson: $"{{\"FileName\": \"{fileName}\", \"Amount\": {amount}, \"CommentsDeleted\": {comments.Count}}}",
                    description: $"Beleg gelöscht: {fileName} (inkl. {comments.Count} Kommentare)"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Fehler beim Löschen des Belegs {receiptId}");
                throw;
            }
        }

    }
}
