using BelegErfassungApp.Data;
using Microsoft.EntityFrameworkCore;

namespace BelegErfassungApp.Services
{
    public class MemberApplicationService : IMemberApplicationService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly IAuditLogService _auditLog;

        public MemberApplicationService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            IAuditLogService auditLog)
        {
            _contextFactory = contextFactory;
            _auditLog = auditLog;
        }

        public async Task<List<MemberApplication>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.MemberApplications
                .Include(m => m.UploadedByUser)
                .Include(m => m.ProcessedByUser)
                .OrderByDescending(m => m.UploadedAt)
                .ToListAsync();
        }

        public async Task<List<MemberApplication>> GetByUserAsync(string userId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.MemberApplications
                .Include(m => m.UploadedByUser)
                .Include(m => m.ProcessedByUser)
                .Where(m => m.UploadedByUserId == userId)
                .OrderByDescending(m => m.UploadedAt)
                .ToListAsync();
        }

        public async Task<MemberApplication?> GetByIdAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.MemberApplications
                .Include(m => m.UploadedByUser)
                .Include(m => m.ProcessedByUser)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<MemberApplication> CreateAsync(MemberApplication application)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.MemberApplications.Add(application);
            await context.SaveChangesAsync();

            await _auditLog.LogAsync(
                application.UploadedByUserId,
                "MemberApplication",
                application.Id.ToString(),
                "Erstellt",
                $"Mitgliedsantrag '{application.FileName}' eingereicht");

            return application;
        }

        public async Task UpdateStatusAsync(int id, MemberApplicationStatus status, string adminUserId, string? note)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var application = await context.MemberApplications.FindAsync(id);
            if (application == null) return;

            var oldStatus = application.Status;
            application.Status = status;
            application.ProcessedByUserId = adminUserId;
            application.ProcessedAt = DateTime.UtcNow;
            application.ProcessingNote = note;

            await context.SaveChangesAsync();

            await _auditLog.LogAsync(
                adminUserId,
                "MemberApplication",
                id.ToString(),
                "StatusGeändert",
                $"Status geändert von '{oldStatus}' zu '{status}'. Notiz: {note}");
        }

        public async Task UpdateFieldsAsync(int id, MemberApplication fields, string adminUserId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var application = await context.MemberApplications.FindAsync(id);
            if (application == null) return;

            application.Nachname = fields.Nachname;
            application.Vorname = fields.Vorname;
            application.Geburtsdatum = fields.Geburtsdatum;
            application.BerufTaetigkeit = fields.BerufTaetigkeit;
            application.Strasse = fields.Strasse;
            application.PLZ = fields.PLZ;
            application.Wohnort = fields.Wohnort;
            application.Telefon = fields.Telefon;
            application.Email = fields.Email;
            application.Antragsdatum = fields.Antragsdatum;
            application.UnterschriftVorhanden = fields.UnterschriftVorhanden;
            application.Kontoinhaber = fields.Kontoinhaber;
            application.Geldinstitut = fields.Geldinstitut;
            application.BIC = fields.BIC;
            application.IBAN = fields.IBAN;
            application.SEPADatum = fields.SEPADatum;
            application.SEPAUnterschriftVorhanden = fields.SEPAUnterschriftVorhanden;

            await context.SaveChangesAsync();

            await _auditLog.LogAsync(
                adminUserId,
                "MemberApplication",
                id.ToString(),
                "FelderBearbeitet",
                $"OCR-Felder manuell bearbeitet von Admin");
        }
        public async Task DeleteAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var application = await context.MemberApplications.FindAsync(id);
            if (application == null) return;

            context.MemberApplications.Remove(application);
            await context.SaveChangesAsync();
        }
    }
}
