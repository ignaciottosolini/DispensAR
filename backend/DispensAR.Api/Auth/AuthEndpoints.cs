using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;

namespace DispensAR.Api.Auth;

public static class AuthEndpoints
{
    public static void MapAccountEndpoints(this WebApplication app)
    {
        var auth = app.MapGroup("/api/auth");
        auth.MapGet("/csrf", (HttpContext context, IAntiforgery antiforgery) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            return Results.Ok(new { token = antiforgery.GetAndStoreTokens(context).RequestToken });
        }).AllowAnonymous();

        auth.MapPost("/login", async (LoginInput input, UserManager<Account> users, SignInManager<Account> signIn) =>
        {
            if (string.IsNullOrWhiteSpace(input.Email) || input.Email.Length > 256 ||
                string.IsNullOrEmpty(input.Password) || input.Password.Length > 1024)
                return Results.Problem("Email o contraseña incorrectos, o cuenta temporalmente bloqueada.", statusCode: 401);
            var account = await users.FindByEmailAsync(input.Email.Trim());
            if (account is null)
                return Results.Problem("Email o contraseña incorrectos, o cuenta temporalmente bloqueada.", statusCode: 401);
            var result = await signIn.PasswordSignInAsync(account, input.Password, isPersistent: false, lockoutOnFailure: true);
            return result.Succeeded ? Results.NoContent() :
                Results.Problem("Email o contraseña incorrectos, o cuenta temporalmente bloqueada.", statusCode: 401);
        }).AllowAnonymous().RequireRateLimiting("login");

        auth.MapGet("/me", async (HttpContext context, UserManager<Account> users) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            var account = await users.GetUserAsync(context.User);
            return account is null ? Results.Unauthorized() :
                Results.Ok(new { account.Id, account.Email, account.TenantId, rol = account.Rol.ToString(), account.EsAdministradorPlataforma });
        }).RequireAuthorization();

        auth.MapPost("/logout", async (SignInManager<Account> signIn) =>
        {
            await signIn.SignOutAsync();
            return Results.NoContent();
        }).RequireAuthorization();
    }
}

record LoginInput(string? Email, string? Password);
