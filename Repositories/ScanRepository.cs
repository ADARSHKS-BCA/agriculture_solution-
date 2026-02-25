using Agriculture.Models;
using Supabase;

namespace Agriculture.Repositories
{
    public class ScanRepository : IScanRepository
    {
        private readonly Client _supabase;
        private readonly ILogger<ScanRepository> _logger;

        public ScanRepository(Client supabase, ILogger<ScanRepository> logger)
        {
            _supabase = supabase;
            _logger = logger;
        }

        public async Task<List<ScanModel>> GetRecentScansAsync(string accessToken, int limit = 10)
        {
            try
            {
                if (!string.IsNullOrEmpty(accessToken))
                    await _supabase.Auth.SetSession(accessToken, "dummy-refresh-token");

                var response = await _supabase.From<ScanModel>()
                                              .Order("created_at", Postgrest.Constants.Ordering.Descending)
                                              .Limit(limit)
                                              .Get();
                
                return response.Models ?? new List<ScanModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve recent scans from Supabase.");
                return new List<ScanModel>();
            }
        }

        public async Task<(int Total, int HighRisk, int ModerateRisk, int LowRisk)> GetDashboardMetricsAsync(string accessToken)
        {
            try
            {
                if (!string.IsNullOrEmpty(accessToken))
                    await _supabase.Auth.SetSession(accessToken, "dummy-refresh-token");

                // Fetching all scans to aggregate metrics. 
                // For a heavily-used production system, Supabase RPC (Remote Procedure Call) 
                // is recommended to calculate these on the Postgres side rather than transferring all rows.
                var response = await _supabase.From<ScanModel>().Get();
                var allScans = response.Models ?? new List<ScanModel>();

                int total = allScans.Count;
                int high = allScans.Count(s => !string.IsNullOrEmpty(s.RiskLevel) && s.RiskLevel.Equals("High", StringComparison.OrdinalIgnoreCase));
                int moderate = allScans.Count(s => !string.IsNullOrEmpty(s.RiskLevel) && s.RiskLevel.Equals("Moderate", StringComparison.OrdinalIgnoreCase));
                int low = allScans.Count(s => string.IsNullOrEmpty(s.RiskLevel) || s.RiskLevel.Equals("Low", StringComparison.OrdinalIgnoreCase));

                return (total, high, moderate, low);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate dashboard metrics from Supabase.");
                return (0, 0, 0, 0);
            }
        }

        public async Task SaveScanAsync(ScanModel scan, string accessToken)
        {
            try
            {
                if (!string.IsNullOrEmpty(accessToken))
                    await _supabase.Auth.SetSession(accessToken, "dummy-refresh-token");

                await _supabase.From<ScanModel>().Insert(scan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save scan result to Supabase.");
            }
        }
    }
}
