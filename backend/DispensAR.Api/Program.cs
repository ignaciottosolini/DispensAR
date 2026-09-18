using System.ComponentModel.DataAnnotations;
using DispensAR.Api.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Antiforgery;
using DispensAR.Api.Data;
using DispensAR.Api.Tenancy;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using DispensAR.Api.Dashboard;
using DispensAR.Api.Pacientes;
using DispensAR.Api.Catalogos;
using DispensAR.Api.Identidad;
using Microsoft.AspNetCore.DataProtection;

var migrate = args.Contains("--migrate");
var createAccount = args.Contains("--create-account");
var assignRole = args.Contains("--assign-role");
var assignPlatformAdmin = args.Contains("--assign-platform-admin");
var builder = WebApplication.CreateBuilder(args.Where(a => a != "--migrate" && a != "--create-account" && a != "--assign-role" && a != "--assign-platform-admin").ToArray());
builder.AddAccountAuthentication();
var keysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(keysPath))
{
    Directory.CreateDirectory(keysPath);
    builder.Services.AddDataProtection().SetApplicationName("DispensAR")
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}
builder.Services.AddProblemDetails();
builder.Services.AddScoped<TenantContext>();
builder.Services.AddSingleton<IAlmacenamientoLogos, AlmacenamientoLogosLocales>();
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(
    DatabaseConfiguration.ConnectionString(builder.Configuration, builder.Environment),
    sql => sql.EnableRetryOnFailure()));
var app = builder.Build();
app.UseExceptionHandler();

// Explicit local maintenance command. Normal startup never changes the schema.
if (migrate)
{
    if (!app.Environment.IsDevelopment())
        throw new InvalidOperationException("--migrate solo esta habilitado en Development.");
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    Console.WriteLine("Migraciones aplicadas. Base DispensAR lista.");
    return;
}
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
if (assignPlatformAdmin)
{
    if (!app.Environment.IsDevelopment()) throw new InvalidOperationException("El cambio local de permiso requiere Development.");
    await using var scope = app.Services.CreateAsyncScope();
    await PlatformAdministration.AssignAsync(scope.ServiceProvider);
    return;
}
if (assignRole)
{
    if (!app.Environment.IsDevelopment()) throw new InvalidOperationException("El cambio local de rol requiere Development.");
    await using var scope = app.Services.CreateAsyncScope();
    await AccountRoles.AssignAsync(scope.ServiceProvider);
    return;
}

if (createAccount)
{
    if (!app.Environment.IsDevelopment())
        throw new InvalidOperationException("El alta local solo esta habilitada en Development.");
    await using var scope = app.Services.CreateAsyncScope();
    await AccountProvisioning.CreateAsync(scope.ServiceProvider);
    return;
}
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseDefaultFiles();
app.UseStaticFiles();
// Validate all cookie-authenticated mutations, including anonymous login, against CSRF.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.Headers.CacheControl = "no-store";
        if (context.Request.Method is not ("GET" or "HEAD" or "OPTIONS"))
        {
            try { await context.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context); }
            catch (AntiforgeryValidationException)
            {
                await Results.Problem("Sesion del formulario vencida. Actualiza la pagina.", statusCode: 400).ExecuteAsync(context);
                return;
            }
        }
    }
    await next(context);
});
app.MapAccountEndpoints();

// Business endpoints use the same authorization and tenant isolation in every environment.
{
    var api = app.MapGroup("/api").RequireAuthorization();
    api.AddEndpointFilter(async (invocation, next) =>
    {
        var services = invocation.HttpContext.RequestServices;
        var account = await services.GetRequiredService<UserManager<Account>>().GetUserAsync(invocation.HttpContext.User);
        if (account is null) return Results.Unauthorized();
        // Never use X-Tenant-Id or input fields as authorization/tenant selection.
        services.GetRequiredService<TenantContext>().Select(account.TenantId);
        var db = services.GetRequiredService<AppDbContext>();
        if (!await db.Organizations.AnyAsync(invocation.HttpContext.RequestAborted))
            return Results.Forbid();
        return await next(invocation);
    });
    api.MapDashboard();
    api.MapWidgetCatalog();
    api.MapPacientes();
    api.MapCatalogos();
    api.MapIdentidadVisual();
    api.MapGet("/workspace", async (AppDbContext db, CancellationToken ct) =>
    {
        var organization = await db.Organizations.AsNoTracking().SingleAsync(ct);
        var branches = await db.Branches.OrderBy(x => x.Name).Select(x => x.Name).ToArrayAsync(ct);
        var users = await db.Profiles.OrderBy(x => x.DisplayName)
            .Select(x => new { x.Id, x.DisplayName, x.Email }).ToArrayAsync(ct);
        return Results.Ok(new { organization.Id, organization.Descripcion, branches, users });
    });
    api.MapGet("/branches", async (AppDbContext db, CancellationToken ct) =>
        Results.Ok(await db.Branches.OrderBy(x => x.Name).Select(x => new { x.Id, x.Name }).ToArrayAsync(ct)));
    api.MapGet("/branches/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
    {
        var branch = await db.Branches.Where(x => x.Id == id).Select(x => new { x.Id, x.Name }).SingleOrDefaultAsync(ct);
        return branch is null ? Results.NotFound() : Results.Ok(branch);
    });
    api.MapPost("/branches", async (BranchInput input, AppDbContext db, CancellationToken ct) =>
    {
        var name = input.Name?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > 160)
            return Results.Problem("El nombre debe tener entre 1 y 160 caracteres.", statusCode: 400);
        var branch = new Branch { TenantId = db.CurrentTenantId, Name = name };
        db.Branches.Add(branch);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/branches/{branch.Id}", new { branch.Id, branch.Name });
    }).RequireAuthorization("TenantOperations");
    api.MapPut("/branches/{id:guid}", async (Guid id, BranchInput input, AppDbContext db, CancellationToken ct) =>
    {
        var name = input.Name?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > 160)
            return Results.Problem("El nombre debe tener entre 1 y 160 caracteres.", statusCode: 400);
        var branch = await db.Branches.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (branch is null) return Results.NotFound();
        branch.Name = name;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }).RequireAuthorization("TenantOperations");
    api.MapDelete("/branches/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
    {
        var branch = await db.Branches.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (branch is null) return Results.NotFound();
        db.Branches.Remove(branch);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }).RequireAuthorization("TenantOperations");
    api.MapGet("/users", async (AppDbContext db, CancellationToken ct) =>
        Results.Ok(await db.Profiles.OrderBy(x => x.DisplayName)
            .Select(x => new { x.Id, x.DisplayName, x.Email }).ToArrayAsync(ct)));
    api.MapGet("/users/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
    {
        var user = await db.Profiles.Where(x => x.Id == id)
            .Select(x => new { x.Id, x.DisplayName, x.Email }).SingleOrDefaultAsync(ct);
        return user is null ? Results.NotFound() : Results.Ok(user);
    });
    api.MapPost("/users", async (UserInput input, AppDbContext db, CancellationToken ct) =>
    {
        var name = input.DisplayName?.Trim();
        var email = input.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(name) || name.Length > 160 || string.IsNullOrEmpty(email) ||
            email.Length > 254 || !new EmailAddressAttribute().IsValid(email))
            return Results.Problem("Indica un nombre (hasta 160 caracteres) y un email valido (hasta 254).", statusCode: 400);
        var user = new OrganizationUser { TenantId = db.CurrentTenantId, DisplayName = name, Email = email };
        db.Profiles.Add(user);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException error) when (error.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return Results.Conflict(new { detail = "Ese email ya existe en esta organizacion." });
        }
        return Results.Created($"/api/users/{user.Id}", new { user.Id, user.DisplayName, user.Email });
    }).RequireAuthorization("TenantOperations");
}
// Unknown API routes must return 404, never the React HTML fallback.
app.MapFallback("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html");
app.Run();
record BranchInput(string? Name);
record UserInput(string? DisplayName, string? Email);



