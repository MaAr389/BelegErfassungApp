using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BelegErfassungApp.Data;
using Microsoft.EntityFrameworkCore;

namespace BelegErfassungApp.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly ApplicationDbContext _context;

        public AuditLogService(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task LogAsync(
            string action,
            string entityType,
            string entityId,
            string actorUserId,
            string? actorEmail = null,
            string? targetUserId = null,
            string? detailsJson = null,
            string? description = null,
            string? ipAddress = null)
        {
            var auditEntry = new AuditLogEntry
            {
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                ActorUserId = actorUserId,
                ActorEmail = actorEmail,
                TargetUserId = targetUserId,
                DetailsJson = detailsJson,
                Description = description,
                IpAddress = ipAddress,
                TimestampUtc = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditEntry);
            await _context.SaveChangesAsync();
        }

        public async Task<List<AuditLogEntry>> GetAuditLogsAsync(
            string? filterAction = null,
            string? filterUserId = null,
            string? filterEntityId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int skip = 0,
            int take = 50)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filterAction))
                query = query.Where(a => a.Action.Contains(filterAction));

            if (!string.IsNullOrWhiteSpace(filterUserId))
                query = query.Where(a => a.ActorUserId == filterUserId || a.TargetUserId == filterUserId);

            if (!string.IsNullOrWhiteSpace(filterEntityId))
                query = query.Where(a => a.EntityId == filterEntityId);

            if (fromDate.HasValue)
                query = query.Where(a => a.TimestampUtc >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(a => a.TimestampUtc <= toDate.Value);

            return await query
                .OrderByDescending(a => a.TimestampUtc)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<AuditLogEntry>> GetLogsForReceiptAsync(int receiptId)
        {
            return await _context.AuditLogs
                .Where(a => a.EntityType == "Receipt" && a.EntityId == receiptId.ToString())
                .OrderByDescending(a => a.TimestampUtc)
                .ToListAsync();
        }

        public async Task<List<AuditLogEntry>> GetLogsForUserAsync(string userId)
        {
            return await _context.AuditLogs
                .Where(a => a.ActorUserId == userId)
                .OrderByDescending(a => a.TimestampUtc)
                .Take(100) // Limit für Performance
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync()
        {
            return await _context.AuditLogs.CountAsync();
        }
    }
}

