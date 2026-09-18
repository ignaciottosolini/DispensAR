using DispensAR.Api.Data;
using Microsoft.EntityFrameworkCore;
using DispensAR.Api.Auth;

namespace DispensAR.Api.Dashboard;

public static class DashboardEndpoints
{
    public static void MapDashboard(this RouteGroupBuilder api)
    {
        api.MapGet("/dashboard", async (AppDbContext db, HttpContext context, CancellationToken ct) =>
        {
            var organization = await db.Organizations.AsNoTracking().SingleAsync(ct);
            var widgets = await Configuration(db, ct);
            return Results.Ok(new
            {
                organization.Id, organization.Descripcion,
                puedeConfigurar = context.User.IsInRole(nameof(TenantRole.Administrador)),
                widgets = widgets.Where(x => x.Habilitado)
            });
        });

        api.MapGet("/dashboard/configuracion", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok(await Configuration(db, ct))).RequireAuthorization("DashboardManagement");

        api.MapPut("/dashboard/configuracion", async (DashboardChanges input, AppDbContext db, CancellationToken ct) =>
        {
            var catalog = await db.DashboardWidgets.AsNoTracking().Where(x => x.Activo).ToListAsync(ct);
            var changes = input.Widgets;
            if (changes is null || changes.Length != catalog.Count || changes.Any(x => x is null) ||
                changes.Select(x => x.Id).Distinct().Count() != changes.Length ||
                changes.Any(x => !catalog.Any(w => w.Id == x.Id) || x.Orden < 0 || x.Orden >= catalog.Count) ||
                changes.Select(x => x.Orden).Distinct().Count() != changes.Length)
                return Results.Problem("Configuración inválida: incluí todos los widgets con un orden único.", statusCode: 400);

            var settings = await db.OrganizationWidgets.ToDictionaryAsync(x => x.WidgetId, ct);
            foreach (var change in changes)
            {
                settings.TryGetValue(change.Id, out var setting);
                var version = setting is null ? null : Convert.ToBase64String(setting.Version);
                if (version != change.Version)
                    return Results.Conflict(new { detail = "La configuración cambió. Recargá antes de guardar." });
                if (setting is null)
                {
                    setting = new OrganizationWidget { TenantId = db.CurrentTenantId, WidgetId = change.Id };
                    db.OrganizationWidgets.Add(setting);
                }
                setting.Habilitado = change.Habilitado;
                setting.Orden = change.Orden;
            }
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateConcurrencyException)
            { return Results.Conflict(new { detail = "Otro administrador modificó el dashboard. Recargá la configuración." }); }
            catch (DbUpdateException exception) when (exception.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 })
            { return Results.Conflict(new { detail = "La configuración cambió. Recargá antes de guardar." }); }
            return Results.Ok(await Configuration(db, ct));
        }).RequireAuthorization("DashboardManagement");

        api.MapGet("/dashboard/widgets/{code}", async (string code, AppDbContext db, CancellationToken ct) =>
        {
            // Check availability on every request; hiding a component is not access control.
            var source = await (from widget in db.DashboardWidgets
                join setting in db.OrganizationWidgets on widget.Id equals setting.WidgetId
                where widget.Codigo == code && widget.Activo && setting.Habilitado
                select widget.TipoIndicador).SingleOrDefaultAsync(ct);
            if (source is null) return Results.NotFound();
            if (source == "sucursales")
            {
                var branches = await db.Branches.AsNoTracking().OrderBy(x => x.Name)
                    .Select(x => x.Name).ToArrayAsync(ct);
                return Results.Ok(new { estado = "disponible", valor = (int?)branches.Length,
                    detalle = "Sucursales registradas en tu asociación", elementos = branches });
            }
            if (source == "pacientes_activos")
            {
                // Only patients explicitly in Activo state; profiles are never counted as patients.
                var pacientes = await db.Pacientes.CountAsync(x => x.Estado == PacienteEstado.Activo, ct);
                return Results.Ok(new { estado = "disponible", valor = (int?)pacientes, unidad = "pacientes",
                    detalle = "Pacientes con estado activo en tu asociación", elementos = Array.Empty<string>() });
            }
            var detail = source switch
            {
                "cantidad_dispensada" => "Las dispensaciones se mostrarán por producto y unidad cuando el módulo esté disponible.",
                "stock_disponible" => "Las existencias se mostrarán por producto y unidad cuando el módulo esté disponible.",
                "alertas" => "Las alertas estarán disponibles al incorporar stock y documentación.",
                "ultimas_dispensaciones" => "Los movimientos aparecerán al implementar el registro de dispensaciones.",
                _ => "Este indicador todavía no tiene una fuente de datos."
            };
            return Results.Ok(new { estado = "sin_datos", valor = (int?)null, detalle = detail, elementos = Array.Empty<string>() });
        });
    }

    private static async Task<WidgetConfiguration[]> Configuration(AppDbContext db, CancellationToken ct)
    {
        var catalog = await db.DashboardWidgets.AsNoTracking().Where(x => x.Activo).OrderBy(x => x.Id).ToListAsync(ct);
        var settings = await db.OrganizationWidgets.AsNoTracking().ToDictionaryAsync(x => x.WidgetId, ct);
        return catalog.Select(widget =>
        {
            settings.TryGetValue(widget.Id, out var setting);
            return new WidgetConfiguration(widget.Id, widget.Codigo, widget.Descripcion, widget.Tamano,
                setting?.Habilitado ?? false, setting?.Orden ?? widget.Id - 1,
                setting is null ? null : Convert.ToBase64String(setting.Version));
        }).OrderBy(x => x.Orden).ThenBy(x => x.Id).ToArray();
    }
}

