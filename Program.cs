using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Supabase;
using System.Text;
using Agriculture.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure JWT Authentication for Supabase
var supabaseUrl = builder.Configuration["Supabase:Url"] ?? throw new InvalidOperationException("Supabase Url not found.");
var authority = $"{supabaseUrl}/auth/v1";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = authority;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidAudiences = new[] { "authenticated" }, // Supabase default audience
            ValidIssuer = authority,
            ValidateIssuer = true, 
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "email",
            RoleClaimType = "role"
        };
        
        // Extract token from cookie if it exists (for standard MVC page navigation without JS fetch)
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Cookies["sb-access-token"];
                if (!string.IsNullOrEmpty(token))
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("JWT Token successfully validated for user: {User}", context.Principal?.Identity?.Name);
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError("JWT Authentication Failed: {Exception}", context.Exception.Message);
                return Task.CompletedTask;
            }
        };
    });

// 2. Configure Supabase Client Singleton
var url = builder.Configuration["Supabase:Url"] ?? throw new InvalidOperationException("Supabase Url not found.");
var key = builder.Configuration["Supabase:AnonKey"] ?? throw new InvalidOperationException("Supabase AnonKey not found.");
var superbaseOptions = new SupabaseOptions { AutoConnectRealtime = false };
builder.Services.AddScoped<Supabase.Client>(_ => new Supabase.Client(url, key, superbaseOptions));

// 3. Register our internal services
builder.Services.AddScoped<DatabaseService>();
builder.Services.AddHttpClient<Agriculture.Services.DiseaseDetectionService>();

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

// Force 401 Unauthorized responses to redirect to the Login page for MVC routes
app.UseStatusCodePages(context =>
{
    if (context.HttpContext.Response.StatusCode == 401)
    {
        context.HttpContext.Response.Redirect("/Auth/Login");
    }
    return Task.CompletedTask;
});

app.UseRouting();

// THESE MUST GO BEFORE MapControllerRoute
app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();
