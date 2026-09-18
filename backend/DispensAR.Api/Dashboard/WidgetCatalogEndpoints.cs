using System.Text.RegularExpressions;
using DispensAR.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace DispensAR.Api.Dashboard;

public record CatalogInput(string? Codigo, string? Descripcion, string? Tamano, string? TipoIndicador, bool Activo, string? Version);
public record CatalogItem(int Id, string Codigo, string Descripcion, string Tamano, string TipoIndicador, bool Activo, string Version);

public static class WidgetCatalogEndpoints
{
    public static void MapWidgetCatalog(this RouteGroupBuilder api)
    {
        // Global catalog. Every endpoint requires an explicitly provisioned platform privilege.
        // This does not grant access to other tenants' clinical or operational records.
        var catalog = api.MapGroup("/plataforma/elementos").RequireAuthorization("PlatformAdministration");
        catalog.MapGet("/", async (AppDbContext db, CancellationToken ct) =>
        {
            var widgets = await db.DashboardWidgets.AsNoTracking().OrderBy(x => x.Descripcion).ToListAsync(ct);
            return Results.Ok(new { elementos = widgets.Select(View), indicadores = DashboardCatalog.Widgets.Select(x => new { codigo = x.TipoIndicador, descripcion = x.Descripcion }) });
        });
        catalog.MapPost("/", async (CatalogInput input, AppDbContext db, CancellationToken ct) =>
        {
            var error = Validate(input);
            if (error is not null) return Results.Problem(error, statusCode: 400);
            var widget = new DashboardWidget { Codigo = input.Codigo!.Trim() };
            Apply(widget, input);
            db.DashboardWidgets.Add(widget);
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateException exception) when (IsDuplicate(exception))
            { return Results.Conflict(new { detail = "Ya existe un elemento con ese código, incluso si está dado de baja." }); }
            return Results.Created($"/api/plataforma/elementos/{widget.Id}", View(widget));
        });
        catalog.MapPut("/{id:int}", async (int id, CatalogInput input, AppDbContext db, CancellationToken ct) =>
        {
            var error = Validate(input);
            if (error is not null) return Results.Problem(error, statusCode: 400);
            var widget = await db.DashboardWidgets.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (widget is null) return Results.NotFound();
            if (widget.Codigo != input.Codigo!.Trim())
                return Results.Problem("El código es permanente. Para otro código, creá un elemento nuevo.", statusCode: 400);
            if (Convert.ToBase64String(widget.Version) != input.Version) return Changed();
            Apply(widget, input);
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateConcurrencyException) { return Changed(); }
            return Results.Ok(View(widget));
        });
        catalog.MapDelete("/{id:int}", async (int id, HttpRequest request, AppDbContext db, CancellationToken ct) =>
        {
            var widget = await db.DashboardWidgets.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (widget is null) return Results.NotFound();
            var version = request.Headers.IfMatch.ToString().Trim('"');
            if (Convert.ToBase64String(widget.Version) != version) return Changed();
            // Logical deletion: retain references and each tenant's preferences for reactivation.
            widget.Activo = false;
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateConcurrencyException) { return Changed(); }
            return Results.NoContent();
        });
    }

    private static CatalogItem View(DashboardWidget widget) => new(widget.Id, widget.Codigo, widget.Descripcion,
        widget.Tamano, widget.TipoIndicador, widget.Activo, Convert.ToBase64String(widget.Version));
    private static IResult Changed() => Results.Conflict(new { detail = "El elemento cambió. Recargá el catálogo antes de continuar." });
    private static bool IsDuplicate(DbUpdateException exception) => exception.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 };
    private static string? Validate(CatalogInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Codigo) || input.Codigo.Trim().Length > 80 ||
            !Regex.IsMatch(input.Codigo.Trim(), "^[a-z][a-z0-9_]*$", RegexOptions.CultureInvariant))
            return "El código debe comenzar con una letra minúscula y contener solo minúsculas, números o guiones bajos (máximo 80).";
        if (string.IsNullOrWhiteSpace(input.Descripcion) || input.Descripcion.Trim().Length > 160)
            return "La descripción debe tener entre 1 y 160 caracteres.";
        if (input.Tamano is not ("normal" or "ancho")) return "El tamaño debe ser normal o ancho.";
        if (!DashboardCatalog.Widgets.Any(x => x.TipoIndicador == input.TipoIndicador)) return "Seleccioná un indicador disponible.";
        return null;
    }
    private static void Apply(DashboardWidget widget, CatalogInput input)
    {
        widget.Descripcion = input.Descripcion!.Trim();
        widget.Tamano = input.Tamano!;
        widget.TipoIndicador = input.TipoIndicador!;
        widget.Activo = input.Activo;
    }
}
