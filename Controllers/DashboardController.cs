using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Agriculture.Models;
using Agriculture.Services;

namespace Agriculture.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly DatabaseService _dbService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(DatabaseService dbService, ILogger<DashboardController> logger)
        {
            _dbService = dbService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var model = new DashboardViewModel();
            try 
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var accessToken = Request.Cookies["sb-access-token"];

                if (!string.IsNullOrEmpty(userIdStr) && Guid.TryParse(userIdStr, out var userId) && !string.IsNullOrEmpty(accessToken))
                {
                    model.UserProfile = await _dbService.GetUserProfileAsync(userId, accessToken);
                }
                
                var history = await _dbService.GetUserHistoryAsync(accessToken ?? string.Empty);
                
                model.TotalScans = history.Count;
                model.HighRiskCases = history.Count(h => !string.IsNullOrEmpty(h.RiskLevel) && h.RiskLevel.Equals("High", StringComparison.OrdinalIgnoreCase));
                model.ModerateRiskCases = history.Count(h => !string.IsNullOrEmpty(h.RiskLevel) && h.RiskLevel.Equals("Medium", StringComparison.OrdinalIgnoreCase));
                model.LowRiskCases = history.Count(h => string.IsNullOrEmpty(h.RiskLevel) || h.RiskLevel.Equals("Low", StringComparison.OrdinalIgnoreCase));
                model.RecentActivity = history.OrderByDescending(h => h.CreatedAt).Take(10).ToList();
                
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user history from Supabase.");
                return View(model);
            }
        }
    }
}
