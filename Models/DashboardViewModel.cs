using System.Collections.Generic;

namespace Agriculture.Models
{
    public class DashboardViewModel
    {
        public ProfileModel? UserProfile { get; set; }
        
        public int TotalScans { get; set; }
        public int HighRiskCases { get; set; }
        public int ModerateRiskCases { get; set; }
        public int LowRiskCases { get; set; }
        
        public List<AnalysisHistoryModel> RecentActivity { get; set; } = new();
    }
}
