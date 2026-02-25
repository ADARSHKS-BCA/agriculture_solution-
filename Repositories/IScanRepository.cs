using Agriculture.Models;

namespace Agriculture.Repositories
{
    public interface IScanRepository
    {
        Task<List<ScanModel>> GetRecentScansAsync(string accessToken, int limit = 10);
        Task<(int Total, int HighRisk, int ModerateRisk, int LowRisk)> GetDashboardMetricsAsync(string accessToken);
        Task SaveScanAsync(ScanModel scan, string accessToken);
    }
}
