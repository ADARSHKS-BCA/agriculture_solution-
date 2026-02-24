using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Agriculture.Models;
using Supabase.Gotrue;

namespace Agriculture.Controllers
{
    public class AuthController : Controller
    {
        private readonly Supabase.Client _supabase;
        private readonly ILogger<AuthController> _logger;

        public AuthController(Supabase.Client supabase, ILogger<AuthController> logger)
        {
            _supabase = supabase;
            _logger = logger;
        }

        // GET: /Auth/Login
        [HttpGet]
        public IActionResult Login()
        {
            // If already authenticated, redirect to Dashboard
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Dashboard");

            return View(new LoginViewModel());
        }

        // POST: /Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var session = await _supabase.Auth.SignIn(model.Email, model.Password);

                if (session == null || string.IsNullOrEmpty(session.AccessToken))
                {
                    ModelState.AddModelError(string.Empty, "Invalid login credentials or email not verified.");
                    return View(model);
                }

                // Strictly store the token via Server-Side HttpOnly Cookie
                SetHttpOnlyCookie("sb-access-token", session.AccessToken, session.ExpiresIn);

                // Ensure the Supabase Refresh Token is also stored securely if we wanted to auto-refresh later
                if (!string.IsNullOrEmpty(session.RefreshToken))
                    SetHttpOnlyCookie("sb-refresh-token", session.RefreshToken, 604800); // 7 days

                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Supabase Login Failed");
                
                // Supabase GoTrue exceptions often contain a ReasonPhrase
                ModelState.AddModelError(string.Empty, "Login failed. Please check your credentials and ensure your email is confirmed.");
                return View(model);
            }
        }

        // GET: /Auth/Signup
        [HttpGet]
        public IActionResult Signup()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Dashboard");

            return View(new SignupViewModel());
        }

        // POST: /Auth/Signup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Signup(SignupViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                // Inject the Username into the User's Supabase Metadata so the Trigger can copy it
                var signUpOptions = new SignUpOptions
                {
                    Data = new Dictionary<string, object>
                    {
                        { "username", model.Username }
                    }
                };

                var session = await _supabase.Auth.SignUp(model.Email, model.Password, signUpOptions);

                // Per Requirements: DO NOT auto-login. Redirect to a success view to mandate email confirmation.
                TempData["SignupSuccess"] = "Account created successfully. Please check your email to confirm your account before logging in.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Supabase Signup Failed");
                ModelState.AddModelError(string.Empty, "Signup failed. This email may already exist or the password is too weak.");
                return View(model);
            }
        }

        // GET/POST: /Auth/Logout
        public async Task<IActionResult> Logout()
        {
            try
            {
                await _supabase.Auth.SignOut();
            }
            catch(Exception ex)
            {
                _logger.LogWarning(ex, "Supabase local signout threw exception");
            }

            // Purge Cookies explicitly
            Response.Cookies.Delete("sb-access-token");
            Response.Cookies.Delete("sb-refresh-token");

            return RedirectToAction("Login");
        }

        // GET: /Auth/Callback
        // Supabase redirects here after email confirmation.
        // It puts the tokens in the URL hash (e.g., #access_token=...&refresh_token=...)
        // Browsers DO NOT send hash fragments to the server, so we must intercept it with a tiny JS snippet.
        [HttpGet]
        public IActionResult Callback()
        {
            return View();
        }

        public class CallbackPayload
        {
            public string access_token { get; set; } = string.Empty;
            public string refresh_token { get; set; } = string.Empty;
            public string expires_in { get; set; } = "3600";
        }

        // POST: /Auth/ProcessCallback
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult ProcessCallback([FromBody] CallbackPayload tokens)
        {
            if (tokens != null && !string.IsNullOrEmpty(tokens.access_token) && 
                                  !string.IsNullOrEmpty(tokens.refresh_token) &&
                                  long.TryParse(tokens.expires_in, out var expiresIn))
            {
                SetHttpOnlyCookie("sb-access-token", tokens.access_token, expiresIn);
                SetHttpOnlyCookie("sb-refresh-token", tokens.refresh_token, 604800);
                return Ok(new { redirectUrl = Url.Action("Index", "Dashboard") });
            }
            
            return BadRequest("Invalid tokens");
        }

        private void SetHttpOnlyCookie(string key, string value, long expireSeconds)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps, // Only force Secure if running on HTTPS (allows http://localhost to work)
                SameSite = SameSiteMode.Lax, // Lax allows cross-site top-level navigations (better for local dev/oauth redirects)
                Expires = DateTime.UtcNow.AddSeconds(expireSeconds)
            };
            Response.Cookies.Append(key, value, cookieOptions);
        }
    }
}
