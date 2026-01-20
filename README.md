# BelegverwaltungApp

Moderne ASP.NET Blazor Server Anwendung zur Belegverwaltung mit OCR-Funktionalität und rollenbasierter Authentifizierung.

## Überblick

Diese Anwendung ermöglicht es Mitgliedern, Belege (Fotos/PDFs) hochzuladen, die automatisch mittels Azure AI Document Intelligence verarbeitet werden. Administratoren können alle Belege einsehen und deren Status verwalten.

## Features

### Für Mitglieder
- ✅ Upload von Belegen (JPG, PNG, PDF)
- ✅ Automatische OCR-Erkennung von:
  - Bruttobetrag
  - Nettobetrag
  - MwSt.-Betrag
  - Belegdatum
- ✅ Manuelle Eingabe von Upload-Datum und Preis
- ✅ Ansicht eigener Belege
- ✅ Status-Übersicht (Offen, In Bearbeitung, Abgeschlossen)

### Für Administratoren
- ✅ Komplette Belegliste aller Mitglieder
- ✅ Status-Verwaltung:
  - Offen
  - In Bearbeitung
  - Abgeschlossen
- ✅ Einsicht in OCR-Daten mit Konfidenzwerten
- ✅ Belegdetails und Vorschau

## Technologie-Stack

- **Framework**: ASP.NET Core 8.0 / Blazor Server
- **Authentifizierung**: ASP.NET Core Identity mit Rollen
- **Datenbank**: SQL Server / Entity Framework Core
- **OCR-Engine**: Azure AI Document Intelligence (prebuilt-receipt)
- **UI**: Bootstrap 5
- **File Storage**: Lokales Dateisystem (kann zu Azure Blob Storage erweitert werden)

## Voraussetzungen

- .NET 8.0 SDK oder höher
- SQL Server (LocalDB, Express oder Full)
- Azure-Abonnement mit Document Intelligence Resource
- Visual Studio 2022 oder Visual Studio Code

## Installation

### 1. Repository klonen

```bash
git clone https://github.com/MaAr389/BelegverwaltungApp.git
cd BelegverwaltungApp
```

### 2. Azure Document Intelligence einrichten

1. Erstelle eine Azure Document Intelligence Resource im Azure Portal
2. Notiere **Endpoint** und **API Key**

### 3. Konfiguration anpassen

Bearbeite `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BelegverwaltungDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "AzureDocumentIntelligence": {
    "Endpoint": "https://YOUR-RESOURCE.cognitiveservices.azure.com/",
    "ApiKey": "YOUR-API-KEY-HERE"
  },
  "FileUpload": {
    "UploadPath": "wwwroot/uploads",
    "MaxFileSizeInMB": 10,
    "AllowedExtensions": ".jpg,.jpeg,.png,.pdf"
  }
}
```

### 4. Datenbank erstellen

```bash
dotnet ef database update
```

Dies erstellt die Datenbank und führt alle Migrationen aus.

### 5. Anwendung starten

```bash
dotnet run
```

Die Anwendung ist unter `https://localhost:5001` erreichbar.

## Erste Schritte

### Admin-Benutzer erstellen

Beim ersten Start der Anwendung wird automatisch ein Admin-Benutzer erstellt:

- **E-Mail**: admin@belegverwaltung.de
- **Passwort**: Admin@123

⚠️ **Wichtig**: Ändere das Passwort nach dem ersten Login!

### Neues Mitglied registrieren

1. Klicke auf "Registrieren"
2. Fülle das Formular aus
3. Neue Benutzer erhalten automatisch die Rolle "Mitglied"

### Beleg hochladen

1. Login als Mitglied
2. Navigiere zu "Meine Belege"
3. Klicke auf "Neuer Beleg"
4. Wähle Datei aus (max. 10 MB)
5. Gebe Belegdatum und manuellen Preis ein
6. Upload - OCR-Verarbeitung erfolgt automatisch

## Projektstruktur

```
BelegverwaltungApp/
│
├── Data/                      # Datenbank-Kontext und Modelle
│   ├── ApplicationDbContext.cs
│   ├── ApplicationUser.cs
│   └── Receipt.cs
│
├── Services/                  # Business Logic
│   ├── IReceiptService.cs
│   ├── ReceiptService.cs
│   ├── IOcrService.cs
│   └── AzureDocumentIntelligenceService.cs
│
├── Pages/                     # Blazor-Seiten
│   ├── Index.razor
│   ├── MyReceipts.razor       # Mitgliederansicht
│   └── Admin/
│       └── AllReceipts.razor  # Administratoransicht
│
├── Migrations/                # EF Core Migrationen
│
├── wwwroot/
│   └── uploads/               # Hochgeladene Belege
│
└── appsettings.json           # Konfiguration
```

## Datenbankmodell

### Receipt (Beleg)

```csharp
public class Receipt
{
    public int Id { get; set; }
    public string UserId { get; set; }
    
    // Upload-Daten
    public DateTime UploadDate { get; set; }
    public string FileName { get; set; }
    public string FilePath { get; set; }
    
    // Manuelle Eingaben
    public DateTime ReceiptDate { get; set; }
    public decimal ManualPrice { get; set; }
    
    // OCR-Ergebnisse
    public decimal? OcrGrossAmount { get; set; }
    public decimal? OcrNetAmount { get; set; }
    public decimal? OcrVatAmount { get; set; }
    public DateTime? OcrReceiptDate { get; set; }
    public double? OcrConfidence { get; set; }
    
    // Status
    public ReceiptStatus Status { get; set; }
    public DateTime? StatusChangedDate { get; set; }
}

public enum ReceiptStatus
{
    Offen = 0,
    InBearbeitung = 1,
    Abgeschlossen = 2
}
```

## API-Endpunkte (optional für Web API-Erweiterung)

Die Anwendung kann einfach um eine REST API erweitert werden:

- `POST /api/receipts` - Beleg hochladen
- `GET /api/receipts` - Eigene Belege abrufen
- `GET /api/receipts/{id}` - Belegdetails
- `PUT /api/receipts/{id}/status` - Status ändern (Admin)
- `GET /api/admin/receipts` - Alle Belege (Admin)

## Sicherheitshinweise

⚠️ **Produktiv-Deployment**:

1. **API-Schlüssel**: Verwende Azure Key Vault für API-Keys
2. **Passwörter**: Ändere Standard-Admin-Passwort
3. **HTTPS**: Erzwinge HTTPS in Produktion
4. **File Upload**: Implementiere Viren-Scan
5. **Rate Limiting**: Begrenze Upload-Frequenz
6. **Datenschutz**: Beachte DSGVO-Anforderungen

## Erweiterungsmöglichkeiten

### Azure Blob Storage Integration

Ersetze lokales Dateisystem durch Azure Blob Storage:

```csharp
// Services/BlobStorageService.cs
public interface IBlobStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName);
    Task<Stream> DownloadFileAsync(string fileName);
}
```

### E-Mail-Benachrichtigungen

Sende E-Mails bei Statusänderungen:

```csharp
// Services/EmailService.cs
public interface IEmailService
{
    Task SendStatusChangeNotificationAsync(string email, Receipt receipt);
}
```

### Excel-Export

Exportiere Belege als Excel-Datei:

```bash
dotnet add package EPPlus
```

### Mobile App

Erstelle eine Xamarin/MAUI-App mit der Web API

## Troubleshooting

### Datenbankverbindung schlägt fehl

```bash
# LocalDB installiert?
sqllocaldb info

# Falls nicht:
sqllocaldb create MSSQLLocalDB
```

### OCR funktioniert nicht

1. Prüfe Azure-Endpoint und API-Key
2. Stelle sicher, dass das Azure-Konto aktiv ist
3. Überprüfe Netzwerkverbindung
4. Prüfe Azure-Quotas

### Uploads schlagen fehl

1. Prüfe `wwwroot/uploads` Ordner existiert
2. Stelle sicher, dass Schreibrechte vorhanden sind
3. Prüfe Dateigröße (max. 10 MB)
4. Prüfe erlaubte Dateitypen

## Performance-Optimierung

### Caching

```csharp
// Program.cs
builder.Services.AddMemoryCache();
builder.Services.AddResponseCaching();
```

### Lazy Loading

```csharp
// ApplicationDbContext.cs
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.UseLazyLoadingProxies();
}
```

### Indexierung

```csharp
modelBuilder.Entity<Receipt>()
    .HasIndex(r => r.UserId);
modelBuilder.Entity<Receipt>()
    .HasIndex(r => r.Status);
```

## Tests

### Unit Tests

```bash
dotnet test
```

### Integration Tests

Siehe `BelegverwaltungApp.Tests` Projekt

## Beitragen

Beiträge sind willkommen! Bitte:

1. Forke das Repository
2. Erstelle einen Feature-Branch (`git checkout -b feature/AmazingFeature`)
3. Committe deine Änderungen (`git commit -m 'Add some AmazingFeature'`)
4. Pushe zum Branch (`git push origin feature/AmazingFeature`)
5. Öffne einen Pull Request

## Lizenz

MIT License - siehe [LICENSE](LICENSE) Datei

## Kontakt

Bei Fragen oder Problemen erstelle bitte ein [Issue](https://github.com/MaAr389/BelegverwaltungApp/issues).

## Changelog

### Version 1.0.0 (2025-11-24)
- ✅ Initiales Release
- ✅ Azure Document Intelligence Integration
- ✅ Identity-Authentifizierung mit Rollen
- ✅ Beleg-Upload und -Verwaltung
- ✅ Admin-Dashboard
- ✅ Status-Workflow

## Roadmap

- [ ] Mobile App (MAUI)
- [ ] Excel-Export
- ✅ E-Mail-Benachrichtigungen
- [ ] Azure Blob Storage
- [ ] Mehrsprachigkeit (i18n)
- [ ] Dark Mode
- [ ] Beleg-Kommentare
- ✅ Audit-Log

---
