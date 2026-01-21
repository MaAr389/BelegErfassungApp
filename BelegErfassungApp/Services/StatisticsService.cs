using BelegErfassungApp.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace BelegErfassungApp.Services
{
    public class StatisticsService : IStatisticsService
    {
        private readonly ApplicationDbContext _context;

        public StatisticsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardStatistics> GetDashboardStatisticsAsync()
        {
            var now = DateTime.UtcNow;
            var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);

            var allReceipts = await _context.Receipts.ToListAsync();

            // Sicherer Average - 0 wenn keine Elemente
            var receiptsWithConfidence = allReceipts.Where(r => r.OcrConfidence.HasValue).ToList();
            var averageConfidence = receiptsWithConfidence.Any()
                ? receiptsWithConfidence.Average(r => r.OcrConfidence!.Value)
                : 0.0;

            return new DashboardStatistics
            {
                TotalAmount = allReceipts.Sum(r => r.ManualPrice),
                TotalReceipts = allReceipts.Count,
                AverageOcrConfidence = averageConfidence,
                ReceiptsThisMonth = allReceipts.Count(r => r.UploadDate >= firstDayOfMonth),
                OpenReceipts = allReceipts.Count(r => r.Status == ReceiptStatus.Offen),
                InProgressReceipts = allReceipts.Count(r => r.Status == ReceiptStatus.InBearbeitung),
                CompletedReceipts = allReceipts.Count(r => r.Status == ReceiptStatus.Abgeschlossen)
            };
        }

        public async Task<List<MonthlyStatistics>> GetMonthlyStatisticsAsync(int year)
        {
            var receipts = await _context.Receipts
                .Where(r => r.UploadDate.Year == year)
                .ToListAsync();

            var monthlyStats = receipts
                .GroupBy(r => r.UploadDate.Month)
                .Select(g => {
                    var receiptsWithConf = g.Where(r => r.OcrConfidence.HasValue).ToList();
                    var avgConf = receiptsWithConf.Any()
                        ? receiptsWithConf.Average(r => r.OcrConfidence!.Value)
                        : 0.0;

                    return new MonthlyStatistics
                    {
                        Year = year,
                        Month = g.Key,
                        MonthName = CultureInfo.GetCultureInfo("de-DE").DateTimeFormat.GetMonthName(g.Key),
                        ReceiptCount = g.Count(),
                        TotalAmount = g.Sum(r => r.ManualPrice),
                        AverageConfidence = avgConf
                    };
                })
                .OrderBy(m => m.Month)
                .ToList();

            return monthlyStats;
        }

        public async Task<List<UserStatistics>> GetUserStatisticsAsync()
        {
            var receipts = await _context.Receipts
                .Include(r => r.User)
                .ToListAsync();

            var userStats = receipts
                .GroupBy(r => new { r.UserId, UserName = r.User!.UserName, Email = r.User.Email })
                .Select(g => {
                    var receiptsWithConf = g.Where(r => r.OcrConfidence.HasValue).ToList();
                    var avgConf = receiptsWithConf.Any()
                        ? receiptsWithConf.Average(r => r.OcrConfidence!.Value)
                        : 0.0;

                    return new UserStatistics
                    {
                        UserId = g.Key.UserId,
                        UserName = g.Key.UserName ?? "Unbekannt",
                        Email = g.Key.Email ?? "",
                        ReceiptCount = g.Count(),
                        TotalAmount = g.Sum(r => r.ManualPrice),
                        AverageConfidence = avgConf
                    };
                })
                .OrderByDescending(u => u.ReceiptCount)
                .ToList();

            return userStats;
        }

        public async Task<UserStatistics> GetUserStatisticsAsync(string userId)
        {
            var now = DateTime.UtcNow;
            var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);

            var userReceipts = await _context.Receipts
                .Include(r => r.User)
                .Where(r => r.UserId == userId)
                .ToListAsync();

            var receiptsWithConfidence = userReceipts.Where(r => r.OcrConfidence.HasValue).ToList();
            var averageConfidence = receiptsWithConfidence.Any()
                ? receiptsWithConfidence.Average(r => r.OcrConfidence!.Value)
                : 0.0;

            var user = userReceipts.FirstOrDefault()?.User;

            return new UserStatistics
            {
                UserId = userId,
                UserName = user?.UserName ?? "Unbekannt",
                Email = user?.Email ?? "",
                ReceiptCount = userReceipts.Count,
                TotalAmount = userReceipts.Sum(r => r.ManualPrice),
                AverageConfidence = averageConfidence,
                // Zusätzliche Properties für User-Dashboard
                TotalReceipts = userReceipts.Count,
                ReceiptsThisMonth = userReceipts.Count(r => r.UploadDate >= firstDayOfMonth),
                OpenReceipts = userReceipts.Count(r => r.Status == ReceiptStatus.Offen),
                InProgressReceipts = userReceipts.Count(r => r.Status == ReceiptStatus.InBearbeitung),
                CompletedReceipts = userReceipts.Count(r => r.Status == ReceiptStatus.Abgeschlossen)
            };
        }


    }
}
