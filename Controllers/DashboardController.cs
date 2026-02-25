using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Agriculture.Models;
using Agriculture.Services;
using Agriculture.Repositories;

namespace Agriculture.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly DatabaseService _dbService;
        private readonly IScanRepository _scanRepository;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(DatabaseService dbService, IScanRepository scanRepository, ILogger<DashboardController> logger)
        {
            _dbService = dbService;
            _scanRepository = scanRepository;
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
                
                // Fetch a large page of historical database records for the History tab
                var historyTable = await _scanRepository.GetRecentScansAsync(accessToken ?? string.Empty, 50);
                
                model.History = historyTable;
                
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
