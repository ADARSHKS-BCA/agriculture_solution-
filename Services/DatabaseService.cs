using Agriculture.Models;
using Supabase;

namespace Agriculture.Services
{
    public class DatabaseService
    {
        private readonly Client _supabase;

        public DatabaseService(Client supabase)
        {
            _supabase = supabase;
        }

        public async Task SaveScanResultAsync(AnalysisHistoryModel model, string accessToken)
        {
            if (!string.IsNullOrEmpty(accessToken))
                await _supabase.Auth.SetSession(accessToken, string.Empty);

            await _supabase.From<AnalysisHistoryModel>().Insert(model);
        }

        public async Task<List<AnalysisHistoryModel>> GetUserHistoryAsync(string accessToken)
        {
            if (!string.IsNullOrEmpty(accessToken))
                await _supabase.Auth.SetSession(accessToken, string.Empty);

            // RLS (Row-Level Security) policies in Supabase automatically filter this 
            // down to ONLY the authenticated user's records.
            var response = await _supabase.From<AnalysisHistoryModel>()
                                          .Order("created_at", Postgrest.Constants.Ordering.Descending)
                                          .Get();
            return response.Models;
        }

        public async Task<ProfileModel?> GetUserProfileAsync(Guid userId, string accessToken)
        {
            if (!string.IsNullOrEmpty(accessToken))
                await _supabase.Auth.SetSession(accessToken, string.Empty);

            var response = await _supabase.From<ProfileModel>()
                                          .Filter("id", Postgrest.Constants.Operator.Equals, userId.ToString())
                                          .Single();
            return response;
        }
    }
}
