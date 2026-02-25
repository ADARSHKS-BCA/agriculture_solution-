using System.Collections.Generic;

namespace Agriculture.Models
{
    public class DashboardViewModel
    {
        public ProfileModel? UserProfile { get; set; }
        
        public List<ScanModel> History { get; set; } = new();
    }
}
