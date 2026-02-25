using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Agriculture.Models;
using Agriculture.Services;

namespace Agriculture.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly DatabaseService _dbService;
        private readonly Supabase.Client _supabase;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(DatabaseService dbService, Supabase.Client supabase, ILogger<ProfileController> logger)
        {
            _dbService = dbService;
            _supabase = supabase;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new ProfileUpdateViewModel();
            try
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var accessToken = Request.Cookies["sb-access-token"];

                if (!string.IsNullOrEmpty(userIdStr) && Guid.TryParse(userIdStr, out var userId) && !string.IsNullOrEmpty(accessToken))
                {
                    var userProfile = await _dbService.GetUserProfileAsync(userId, accessToken);
                    if (userProfile != null)
                    {
                        model.Username = userProfile.Username ?? string.Empty;
                        // Use the CurrentUser email natively from Gotrue if missing from Profile table
                        model.Email = _supabase.Auth.CurrentUser?.Email ?? userProfile.Email ?? string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user profile for the settings page.");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ProfileUpdateViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var accessToken = Request.Cookies["sb-access-token"];

                if (string.IsNullOrEmpty(userIdStr) || string.IsNullOrEmpty(accessToken))
                {
                    TempData["ProfileError"] = "Authentication session lost. Please log in again.";
                    return RedirectToAction("Login", "Auth");
                }

                // Inject the user's JWT into the Supabase client context
                await _supabase.Auth.SetSession(accessToken, "dummy-refresh-token");

                // 1. Update the Username in Postgres (public.profiles)
                var currentProfile = await _supabase.From<ProfileModel>()
                                           .Filter("id", Postgrest.Constants.Operator.Equals, userIdStr)
                                           .Single();

                if (currentProfile != null)
                {
                    currentProfile.Username = model.Username;
                    await currentProfile.Update<ProfileModel>();
                }

                // 2. Update the Password in GoTrue (auth.users) if provided
                if (!string.IsNullOrEmpty(model.NewPassword))
                {
                    var userAttrs = new Supabase.Gotrue.UserAttributes 
                    { 
                        Password = model.NewPassword 
                    };
                    await _supabase.Auth.Update(userAttrs);
                }

                TempData["ProfileSuccess"] = "Profile updated successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile details.");
                TempData["ProfileError"] = "Failed to update profile. " + ex.Message;
                return View(model);
            }
        }
    }
}
