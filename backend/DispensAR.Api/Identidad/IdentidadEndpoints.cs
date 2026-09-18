using DispensAR.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace DispensAR.Api.Identidad;

public static class IdentidadEndpoints
{
    // Colors every tenant falls back to when a value is missing. They mirror the
    // :root defaults in frontend/src/styles/tokens.css and theme/tema.ts.
    public const string PredeterminadoPrimario = "#195d4b";
    public const string PredeterminadoSecundario = "#2e6b52";
    public const string PredeterminadoSidebarFondo = "#ffffff";
    public const string PredeterminadoSidebarTexto = "#526d60";
    private const string Superficie = "#ffffff";
    private const string FondoAplicacion = "#eff5f2";

    public static void MapIdentidadVisual(this RouteGroupBuilder api)
    {
        // Every authenticated role reads the identity: Administrativo and UsuarioTenant
        // apply the configured theme without being able to edit it.
        api.MapGet("/identidad", async (AppDbContext db, CancellationToken ct) =>
        {
            var organization = await db.Organizations.AsNoTracking().SingleAsync(ct);
            var config = await db.IdentidadesVisuales.AsNoTracking().SingleOrDefaultAsync(ct);
            return Results.Ok(new IdentidadView(organization.Descripcion, config is null ? null : Vista(config)));
        });

        // Logos for the session tenant, saved or just uploaded for preview. The tenant in
        // the path must equal the session tenant, so another organization's file 404s.
        api.MapGet("/identidad/logos/{tenantId:int}/{tipo}/{archivo}", async (int tenantId, string tipo,
            string archivo, AppDbContext db, IAlmacenamientoLogos almacen, HttpContext contexto, CancellationToken ct) =>
        {
            if (tenantId != db.CurrentTenantId) return Results.NotFound();
            var abierta = await almacen.AbrirAsync($"{tenantId}/{tipo}/{archivo}", ct);
            if (abierta is null) return Results.NotFound();
            contexto.Response.Headers.CacheControl = "private, max-age=300";
            return Results.File(abierta.Value.Contenido, abierta.Value.ContentType);
        });

        var identidad = api.MapGroup("/identidad").RequireAuthorization("VisualIdentity");

        identidad.MapPut("/", async (IdentidadInput input, AppDbContext db, IAlmacenamientoLogos almacen,
            CancellationToken ct) =>
        {
            if (!Normalizar(input.ColorPrimario, out var colorPrimario, out var error)) return Problema(error);
            if (!Normalizar(input.ColorSecundario, out var colorSecundario, out error)) return Problema(error);
            if (!Normalizar(input.ColorSidebarFondo, out var colorSidebarFondo, out error)) return Problema(error);
            if (!Normalizar(input.ColorSidebarTexto, out var colorSidebarTexto, out error)) return Problema(error);
            var primario = Configurada(colorPrimario);
            var secundario = Configurada(colorSecundario);
            var sidebarFondo = Configurada(colorSidebarFondo);
            var sidebarTexto = Configurada(colorSidebarTexto);
            // Contrast is always checked on the effective combination, so a single color
            // cannot break readability against the defaults of the others.
            error = Contrastear(primario ?? PredeterminadoPrimario, secundario ?? PredeterminadoSecundario,
                sidebarFondo ?? PredeterminadoSidebarFondo, sidebarTexto ?? PredeterminadoSidebarTexto) ?? string.Empty;
            if (!string.IsNullOrEmpty(error)) return Problema(error);
            var tenantId = db.CurrentTenantId;
            foreach (var (ruta, tipo) in new[] { (input.LogoPrincipalRuta, RutasLogo.Principal), (input.LogoCompactoRuta, RutasLogo.Compacto) })
            {
                if (ruta is null) continue;
                var partes = ruta.Split('/');
                if (!RutasLogo.PerteneceA(tenantId, ruta) || partes.Length != 3 || partes[1] != tipo ||
                    !await almacen.ExisteAsync(ruta, ct))
                    return Problema("El logo indicado no pertenece a tu asociación o ya no está disponible. Volvé a subirlo.");
            }
            var config = await db.IdentidadesVisuales.SingleOrDefaultAsync(x => x.TenantId == tenantId, ct);
            var versionActual = config is null ? null : Convert.ToBase64String(config.Version);
            if (input.Version != versionActual)
                return Results.Conflict(new { detail = "La identidad visual cambió. Recargá antes de guardar." });
            var reemplazados = new List<string>();
            var existia = config is not null;
            config ??= new IdentidadVisual { TenantId = tenantId };
            if (existia)
            {
                if (EsReemplazo(config.LogoPrincipalRuta, input.LogoPrincipalRuta)) reemplazados.Add(config.LogoPrincipalRuta!);
                if (EsReemplazo(config.LogoCompactoRuta, input.LogoCompactoRuta)) reemplazados.Add(config.LogoCompactoRuta!);
            }
            config.ColorPrimario = primario;
            config.ColorSecundario = secundario;
            config.ColorSidebarFondo = sidebarFondo;
            config.ColorSidebarTexto = sidebarTexto;
            config.LogoPrincipalRuta = input.LogoPrincipalRuta;
            config.LogoPrincipalContenido = input.LogoPrincipalRuta is null ? null : ContentType(input.LogoPrincipalRuta);
            config.LogoCompactoRuta = input.LogoCompactoRuta;
            config.LogoCompactoContenido = input.LogoCompactoRuta is null ? null : ContentType(input.LogoCompactoRuta);
            var vacia = (primario, secundario, sidebarFondo, sidebarTexto, input.LogoPrincipalRuta, input.LogoCompactoRuta)
                is (null, null, null, null, null, null);
            // A configuration that only repeats the defaults is stored as "no configuration"
            // so the tenant keeps following future changes to the DispensAR theme.
            if (vacia)
            {
                if (existia) db.IdentidadesVisuales.Remove(config);
            }
            else if (!existia)
            {
                db.IdentidadesVisuales.Add(config);
            }
            // existia && !vacia: the tracked entity already carries the new values.
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateConcurrencyException)
            { return Results.Conflict(new { detail = "La identidad visual cambió. Recargá antes de guardar." }); }
            foreach (var reemplazado in reemplazados) await almacen.EliminarAsync(reemplazado, ct);
            return Results.Ok(vacia ? null : Vista(config));
        });

        identidad.MapDelete("/", async (HttpRequest request, AppDbContext db, IAlmacenamientoLogos almacen,
            CancellationToken ct) =>
        {
            var tenantId = db.CurrentTenantId;
            var config = await db.IdentidadesVisuales.SingleOrDefaultAsync(x => x.TenantId == tenantId, ct);
            // Restoring defaults when nothing is configured is a no-op.
            if (config is null) return Results.NoContent();
            var version = request.Headers.IfMatch.ToString().Trim('"');
            if (Convert.ToBase64String(config.Version) != version)
                return Results.Conflict(new { detail = "La identidad visual cambió. Recargá antes de continuar." });
            var archivos = new[] { config.LogoPrincipalRuta, config.LogoCompactoRuta };
            db.IdentidadesVisuales.Remove(config);
            await db.SaveChangesAsync(ct);
            foreach (var archivo in archivos) await almacen.EliminarAsync(archivo, ct);
            return Results.NoContent();
        });

        identidad.MapPost("/logos/{tipo}", async (string tipo, IFormFile? archivo, AppDbContext db,
            IAlmacenamientoLogos almacen, HttpContext contexto, CancellationToken ct) =>
        {
            if (!RutasLogo.EsTipoValido(tipo)) return Problema("El tipo de logo debe ser principal o compacto.");
            if (archivo is null || archivo.Length == 0) return Problema("Seleccioná un archivo de imagen.");
            if (archivo.Length > InspectorImagen.MaximoBytes)
                return Problema($"El logo no puede superar {InspectorImagen.MaximoBytes / 1024} KB.");
            using var memoria = new MemoryStream((int)archivo.Length);
            await archivo.CopyToAsync(memoria, ct);
            var inspectada = InspectorImagen.Inspeccionar(memoria.GetBuffer().AsSpan(0, (int)memoria.Length));
            if (inspectada is null) return Problema("El archivo debe ser una imagen PNG, JPEG o WebP válida.");
            if (!InspectorImagen.DentroDeLimites(inspectada))
                return Problema($"El logo no puede superar {InspectorImagen.MaximoLado} × {InspectorImagen.MaximoLado} píxeles.");
            memoria.Position = 0;
            var guardado = await almacen.GuardarAsync(db.CurrentTenantId, tipo, memoria, inspectada.Extension, ct);
            contexto.Response.Headers.CacheControl = "no-store";
            return Results.Ok(new { guardado.Ruta, Url = Url(guardado.Ruta) });
        }).DisableAntiforgery();
    }

    private static bool Normalizar(string? valor, out string normalizado, out string error)
    {
        normalizado = string.Empty;
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(valor)) return true;
        if (Colores.EsHexadecimal(valor, out var hex)) { normalizado = hex; return true; }
        error = "Los colores deben escribirse como #rgb o #rrggbb con dígitos hexadecimales.";
        return false;
    }

    // Empty means "not configured": the stored value stays null and the theme falls back
    // to the DispensAR default.
    private static string? Configurada(string normalizado) => normalizado.Length == 0 ? null : normalizado;

    private static string? Contrastear(string primario, string secundario, string sidebarFondo, string sidebarTexto)
    {
        if (Colores.Contraste(primario, Superficie) < 4.5)
            return "El color primario necesita un contraste de al menos 4.5:1 sobre blanco para que el texto de los botones sea legible.";
        if (Colores.Contraste(secundario, Superficie) < 3 || Colores.Contraste(secundario, FondoAplicacion) < 3)
            return "El color secundario necesita un contraste de al menos 3:1 sobre el fondo de la aplicación.";
        if (Colores.Contraste(sidebarTexto, sidebarFondo) < 4.5)
            return "El texto de la barra lateral necesita un contraste de al menos 4.5:1 sobre su fondo.";
        return null;
    }

    private static bool EsReemplazo(string? actual, string? nueva) => actual is not null && actual != nueva;

    private static string ContentType(string ruta) => Path.GetExtension(ruta).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".jpg" => "image/jpeg",
        ".webp" => "image/webp",
        _ => "application/octet-stream",
    };

    // Las columnas nulas significan "usa el predeterminado de DispensAR". El frontend
    // solo define la variable CSS correspondiente cuando el tenant configuró el valor.
    private static IdentidadVisualView Vista(IdentidadVisual config) => new(
        config.LogoPrincipalRuta is null ? null : Url(config.LogoPrincipalRuta),
        config.LogoCompactoRuta is null ? null : Url(config.LogoCompactoRuta),
        config.ColorPrimario,
        config.ColorSecundario,
        config.ColorSidebarFondo,
        config.ColorSidebarTexto,
        config.Version.Length == 0 ? "" : Convert.ToBase64String(config.Version));

    private static string Url(string ruta) => $"/api/identidad/logos/{ruta}";

    private static IResult Problema(string detalle) => Results.Problem(detalle, statusCode: 400);
}
