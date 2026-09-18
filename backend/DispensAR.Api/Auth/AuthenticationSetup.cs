using DispensAR.Api.Data;
using Microsoft.AspNetCore.Identity;
using System.Threading.RateLimiting;

namespace DispensAR.Api.Auth;

public static class AuthenticationSetup
{
    public static void AddAccountAuthentication(this WebApplicationBuilder builder)
    {
        builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
        builder.Services.AddIdentityCore<Account>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 12;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        }).AddEntityFrameworkStores<AppDbContext>().AddSignInManager().AddDefaultTokenProviders()
            .AddClaimsPrincipalFactory<AccountClaimsFactory>();
        builder.Services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero);
        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "DispensAR.Session";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = false;
            options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = 401; return Task.CompletedTask; };
            options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; };
        });
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("PlatformAdministration", policy => policy.RequireClaim("platform.administrator", "true"));
            options.AddPolicy("DashboardManagement", policy => policy.RequireRole(nameof(TenantRole.Administrador)));
            options.AddPolicy("TenantOperations", policy => policy.RequireRole(nameof(TenantRole.Administrador), nameof(TenantRole.Administrativo)));
            options.AddPolicy("VisualIdentity", policy => policy.RequireRole(nameof(TenantRole.Administrador)));
        });
        builder.Services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "DispensAR.Antiforgery";
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        });
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
            options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
                }));
        });
    }
}
