using BelegErfassungApp.Data;

namespace BelegErfassungApp.Services
{
    public interface IStatisticsService
    {
        Task<DashboardStatistics> GetDashboardStatisticsAsync();
        Task<List<MonthlyStatistics>> GetMonthlyStatisticsAsync(int year);
        Task<List<UserStatistics>> GetUserStatisticsAsync();
        Task<UserStatistics> GetUserStatisticsAsync(string userId);

    }

    public class DashboardStatistics
    {
        public decimal TotalAmount { get; set; }
        public int TotalReceipts { get; set; }
        public double AverageOcrConfidence { get; set; }
        public int ReceiptsThisMonth { get; set; }
        public int OpenReceipts { get; set; }
        public int InProgressReceipts { get; set; }
        public int CompletedReceipts { get; set; }
    }

    public class MonthlyStatistics
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int ReceiptCount { get; set; }
        public decimal TotalAmount { get; set; }
        public double AverageConfidence { get; set; }
    }

    public class UserStatistics
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int ReceiptCount { get; set; }
        public decimal TotalAmount { get; set; }
        public double AverageConfidence { get; set; }

        // Zusätzlich für User-Dashboard
        public int TotalReceipts { get; set; }
        public int ReceiptsThisMonth { get; set; }
        public int OpenReceipts { get; set; }
        public int InProgressReceipts { get; set; }
        public int CompletedReceipts { get; set; }
    }
}
