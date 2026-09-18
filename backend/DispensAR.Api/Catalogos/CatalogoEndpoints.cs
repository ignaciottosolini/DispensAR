using System.Text.RegularExpressions;
using DispensAR.Api.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DispensAR.Api.Catalogos;

public record ProductoInput(string? Codigo, string? Descripcion, string? Unidad, bool Activo, string? Version);
public record ProductoView(Guid Id, string Codigo, string Descripcion, string Unidad, bool Activo, string Version);
public record LoteInput(Guid? ProductoId, string? Codigo, DateOnly? FechaVencimiento, bool Activo, string? Version);
public record LoteView(Guid Id, Guid ProductoId, string ProductoCodigo, string ProductoDescripcion, string ProductoUnidad,
    string Codigo, DateOnly? FechaVencimiento, bool Activo, string Version);

public static class CatalogoEndpoints
{
    public static void MapCatalogos(this RouteGroupBuilder api)
    {
        api.MapGet("/productos", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok((await db.Productos.AsNoTracking().OrderBy(x => x.Descripcion).ToArrayAsync(ct)).Select(Viewed).ToArray()));
        api.MapPost("/productos", async (ProductoInput input, AppDbContext db, CancellationToken ct) =>
        {
            var error = ValidateProducto(input);
            if (error is not null) return Results.Problem(error, statusCode: 400);
            var producto = new Producto { Codigo = input.Codigo!.Trim() };
            Apply(producto, input);
            db.Productos.Add(producto);
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateException exception) when (IsDuplicate(exception))
            { return Results.Conflict(new { detail = "Ya existe un producto con ese código en tu asociación, incluso si está dado de baja." }); }
            return Results.Created($"/api/productos/{producto.Id}", Viewed(producto));
        }).RequireAuthorization("TenantOperations");
        api.MapPut("/productos/{id:guid}", async (Guid id, ProductoInput input, AppDbContext db, CancellationToken ct) =>
        {
            var error = ValidateProducto(input);
            if (error is not null) return Results.Problem(error, statusCode: 400);
            var producto = await db.Productos.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (producto is null) return Results.NotFound();
            if (producto.Codigo != input.Codigo!.Trim())
                return Results.Problem("El código es permanente. Para otro código, creá un producto nuevo.", statusCode: 400);
            if (producto.Unidad.ToString() != input.Unidad &&
                await db.Lotes.AnyAsync(x => x.ProductoId == id, ct))
                return Results.Problem("El producto ya tiene lotes registrados y su unidad no puede cambiar.", statusCode: 400);
            if (Convert.ToBase64String(producto.Version) != input.Version)
                return Results.Conflict(new { detail = "El producto cambió. Recargá antes de guardar." });
            Apply(producto, input);
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateConcurrencyException)
            { return Results.Conflict(new { detail = "El producto cambió. Recargá antes de guardar." }); }
            return Results.Ok(Viewed(producto));
        }).RequireAuthorization("TenantOperations");
        api.MapDelete("/productos/{id:guid}", async (Guid id, HttpRequest request, AppDbContext db, CancellationToken ct) =>
        {
            var producto = await db.Productos.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (producto is null) return Results.NotFound();
            var version = request.Headers.IfMatch.ToString().Trim('"');
            if (Convert.ToBase64String(producto.Version) != version)
                return Results.Conflict(new { detail = "El producto cambió. Recargá antes de continuar." });
            // Logical deletion: existing lots keep their product reference.
            producto.Activo = false;
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateConcurrencyException)
            { return Results.Conflict(new { detail = "El producto cambió. Recargá antes de continuar." }); }
            return Results.NoContent();
        }).RequireAuthorization("TenantOperations");

        api.MapGet("/lotes", async (AppDbContext db, CancellationToken ct) =>
        {
            var lotes = await db.Lotes.AsNoTracking().OrderBy(x => x.Codigo).ToArrayAsync(ct);
            var productos = await db.Productos.AsNoTracking().ToDictionaryAsync(x => x.Id, ct);
            return Results.Ok(lotes.Select(lote => Viewed(lote, productos.GetValueOrDefault(lote.ProductoId)))
                .ToArray());
        });
        api.MapPost("/lotes", async (LoteInput input, AppDbContext db, CancellationToken ct) =>
        {
            var error = ValidateLote(input);
            if (error is not null) return Results.Problem(error, statusCode: 400);
            var producto = await db.Productos.SingleOrDefaultAsync(x => x.Id == input.ProductoId, ct);
            if (producto is null || !producto.Activo)
                return Results.Problem("Seleccioná un producto activo de tu asociación.", statusCode: 400);
            var lote = new Lote { ProductoId = producto.Id, Codigo = input.Codigo!.Trim() };
            Apply(lote, input);
            db.Lotes.Add(lote);
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateException exception) when (IsDuplicate(exception))
            { return Results.Conflict(new { detail = "Ya existe un lote con ese código para este producto, incluso si está dado de baja." }); }
            return Results.Created($"/api/lotes/{lote.Id}", Viewed(lote, producto));
        }).RequireAuthorization("TenantOperations");
        api.MapPut("/lotes/{id:guid}", async (Guid id, LoteInput input, AppDbContext db, CancellationToken ct) =>
        {
            var error = ValidateLote(input);
            if (error is not null) return Results.Problem(error, statusCode: 400);
            var lote = await db.Lotes.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (lote is null) return Results.NotFound();
            if (lote.Codigo != input.Codigo!.Trim())
                return Results.Problem("El código del lote es permanente. Para otro código, registrá un lote nuevo.", statusCode: 400);
            if (input.ProductoId is not null && input.ProductoId != lote.ProductoId)
                return Results.Problem("Un lote no puede cambiar de producto.", statusCode: 400);
            if (Convert.ToBase64String(lote.Version) != input.Version)
                return Results.Conflict(new { detail = "El lote cambió. Recargá antes de guardar." });
            Apply(lote, input);
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateConcurrencyException)
            { return Results.Conflict(new { detail = "El lote cambió. Recargá antes de guardar." }); }
            var producto = await db.Productos.AsNoTracking().SingleAsync(x => x.Id == lote.ProductoId, ct);
            return Results.Ok(Viewed(lote, producto));
        }).RequireAuthorization("TenantOperations");
        api.MapDelete("/lotes/{id:guid}", async (Guid id, HttpRequest request, AppDbContext db, CancellationToken ct) =>
        {
            var lote = await db.Lotes.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (lote is null) return Results.NotFound();
            var version = request.Headers.IfMatch.ToString().Trim('"');
            if (Convert.ToBase64String(lote.Version) != version)
                return Results.Conflict(new { detail = "El lote cambió. Recargá antes de continuar." });
            lote.Activo = false;
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateConcurrencyException)
            { return Results.Conflict(new { detail = "El lote cambió. Recargá antes de continuar." }); }
            return Results.NoContent();
        }).RequireAuthorization("TenantOperations");
    }

    private static bool IsDuplicate(DbUpdateException exception) => exception.InnerException is SqlException { Number: 2601 or 2627 };
    private static ProductoView Viewed(Producto producto) => new(producto.Id, producto.Codigo, producto.Descripcion,
        producto.Unidad.ToString(), producto.Activo, Convert.ToBase64String(producto.Version));
    private static LoteView Viewed(Lote lote, Producto? producto) => new(lote.Id, lote.ProductoId,
        producto?.Codigo ?? "", producto?.Descripcion ?? "Producto eliminado", producto?.Unidad.ToString() ?? "",
        lote.Codigo, lote.FechaVencimiento, lote.Activo, Convert.ToBase64String(lote.Version));

    private static void Apply(Producto producto, ProductoInput input)
    {
        producto.Descripcion = input.Descripcion!.Trim();
        producto.Unidad = Enum.Parse<ProductoUnidad>(input.Unidad!);
        producto.Activo = input.Activo;
    }
    private static void Apply(Lote lote, LoteInput input)
    {
        lote.FechaVencimiento = input.FechaVencimiento;
        lote.Activo = input.Activo;
    }

    private static string? ValidateProducto(ProductoInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Codigo) || input.Codigo.Trim().Length > 80 ||
            !Regex.IsMatch(input.Codigo.Trim(), "^[a-z][a-z0-9_]*$", RegexOptions.CultureInvariant))
            return "El código debe comenzar con una letra minúscula y contener solo minúsculas, números o guiones bajos (máximo 80).";
        if (string.IsNullOrWhiteSpace(input.Descripcion) || input.Descripcion.Trim().Length > 160)
            return "La descripción debe tener entre 1 y 160 caracteres.";
        if (input.Unidad is not ("gramos" or "mililitros" or "unidades"))
            return "La unidad debe ser gramos, mililitros o unidades.";
        return null;
    }

    private static string? ValidateLote(LoteInput input)
    {
        if (input.ProductoId is null) return "Seleccioná un producto.";
        if (string.IsNullOrWhiteSpace(input.Codigo) || input.Codigo.Trim().Length > 80 ||
            !Regex.IsMatch(input.Codigo.Trim(), "^[0-9A-Za-z][0-9A-Za-z._/-]{0,79}$", RegexOptions.CultureInvariant))
            return "El código del lote admite letras, números, punto, guion, guion bajo y barra (máximo 80).";
        return null;
    }
}
