using Azure;
using Azure.AI.DocumentIntelligence;
using BelegErfassungApp.Data;

namespace BelegErfassungApp.Services
{
    public interface IOcrService
    {
        Task<OcrResult> AnalyzeReceiptAsync(Stream fileStream);
    }

    public class OcrResult
    {
        public decimal? GrossAmount { get; set; }
        public decimal? NetAmount { get; set; }
        public decimal? VatAmount { get; set; }
        public DateTime? ReceiptDate { get; set; }
        public double? Confidence { get; set; }
        public string? MerchantName { get; set; }
        public string? MerchantAddress { get; set; }
    }

    public class AzureDocumentIntelligenceService : IOcrService
    {
        private readonly DocumentIntelligenceClient _client;
        private readonly ILogger<AzureDocumentIntelligenceService> _logger;

        public AzureDocumentIntelligenceService(
            IConfiguration configuration,
            ILogger<AzureDocumentIntelligenceService> logger)
        {
            var endpoint = configuration["AzureDocumentIntelligence:Endpoint"];
            var apiKey = configuration["AzureDocumentIntelligence:ApiKey"];

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException(
                    "Azure Document Intelligence Endpoint und ApiKey müssen in appsettings.json konfiguriert sein.");
            }

            _client = new DocumentIntelligenceClient(
                new Uri(endpoint),
                new AzureKeyCredential(apiKey));
            _logger = logger;
        }

        // *** DRITTE VARIANTE - HIER ***
        public async Task<OcrResult> AnalyzeReceiptAsync(Stream fileStream)
        {
            try
            {
                _logger.LogInformation("Starte OCR-Analyse mit Azure Document Intelligence");

                // Stream direkt in BinaryData konvertieren
                var binaryData = await BinaryData.FromStreamAsync(fileStream);

                // Direkt mit BinaryData analysieren (einfachste API)
                var operation = await _client.AnalyzeDocumentAsync(
                    WaitUntil.Completed,
                    "prebuilt-receipt",
                    binaryData);

                var result = operation.Value;
                var ocrResult = new OcrResult();

                if (result.Documents?.Count > 0)
                {
                    var document = result.Documents[0];
                    _logger.LogInformation($"Dokument erkannt. Felder: {document.Fields.Count}");

                    // Total (Bruttobetrag)
                    if (document.Fields.TryGetValue("Total", out var totalField) &&
                        totalField.ValueCurrency != null)
                    {
                        ocrResult.GrossAmount = (decimal)totalField.ValueCurrency.Amount;
                        ocrResult.Confidence = totalField.Confidence;
                        _logger.LogInformation($"Bruttobetrag: {ocrResult.GrossAmount:C} (Konfidenz: {totalField.Confidence:P})");
                    }

                    // Subtotal (Nettobetrag)
                    if (document.Fields.TryGetValue("Subtotal", out var subtotalField) &&
                        subtotalField.ValueCurrency != null)
                    {
                        ocrResult.NetAmount = (decimal)subtotalField.ValueCurrency.Amount;
                        _logger.LogInformation($"Nettobetrag: {ocrResult.NetAmount:C}");
                    }

                    // TotalTax (MwSt.)
                    if (document.Fields.TryGetValue("TotalTax", out var taxField) &&
                        taxField.ValueCurrency != null)
                    {
                        ocrResult.VatAmount = (decimal)taxField.ValueCurrency.Amount;
                        _logger.LogInformation($"MwSt.: {ocrResult.VatAmount:C}");
                    }

                    // TransactionDate (Belegdatum)
                    if (document.Fields.TryGetValue("TransactionDate", out var dateField) &&
                        dateField.ValueDate != null)
                    {
                        ocrResult.ReceiptDate = dateField.ValueDate.Value.DateTime;
                        _logger.LogInformation($"Belegdatum: {ocrResult.ReceiptDate:d}");
                    }

                    // MerchantName (Händlername)
                    if (document.Fields.TryGetValue("MerchantName", out var merchantField))
                    {
                        ocrResult.MerchantName = merchantField.ValueString;
                        _logger.LogInformation($"Händler: {ocrResult.MerchantName}");
                    }

                    // MerchantAddress (Händleradresse)
                    if (document.Fields.TryGetValue("MerchantAddress", out var addressField))
                    {
                        ocrResult.MerchantAddress = addressField.ValueString;
                    }

                    _logger.LogInformation("OCR-Analyse erfolgreich abgeschlossen");
                }
                else
                {
                    _logger.LogWarning("Keine Dokumente in OCR-Ergebnis gefunden");
                }

                return ocrResult;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, $"Azure Document Intelligence API-Fehler: {ex.Message}");
                return new OcrResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Allgemeiner Fehler bei der OCR-Verarbeitung");
                return new OcrResult();
            }
        }
    }
}
