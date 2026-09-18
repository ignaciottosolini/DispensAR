using System.Text.RegularExpressions;

namespace DispensAR.Api.Identidad;

public sealed record LogoGuardado(string Ruta, string ContentType);

// Server-side generated names. The tenant owns the folder, so a stored path can never
// escape its own directory or collide with another organization.
public static class RutasLogo
{
    public const string Principal = "principal";
    public const string Compacto = "compacto";
    private static readonly Regex Seguro = new(@"^(\d{1,9})/(principal|compacto)/([0-9a-f]{32})\.(png|jpg|webp)$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool EsValido(string? ruta) => ruta is not null && Seguro.IsMatch(ruta);
    public static bool EsTipoValido(string? tipo) => tipo is Principal or Compacto;

    public static string Nuevo(int tenantId, string tipo, string extension)
    {
        if (!EsTipoValido(tipo)) throw new ArgumentException("Tipo de logo desconocido.", nameof(tipo));
        return $"{tenantId}/{tipo}/{Guid.NewGuid():N}{extension}";
    }

    public static bool PerteneceA(int tenantId, string ruta) =>
        EsValido(ruta) && ruta.StartsWith($"{tenantId}/", StringComparison.Ordinal);
}

public interface IAlmacenamientoLogos
{
    Task<LogoGuardado> GuardarAsync(int tenantId, string tipo, Stream contenido, string extension, CancellationToken ct);
    Task<bool> ExisteAsync(string ruta, CancellationToken ct);
    Task<(Stream Contenido, string ContentType)?> AbrirAsync(string ruta, CancellationToken ct);
    Task EliminarAsync(string? ruta, CancellationToken ct);
}

// Local disk storage. Swap this registration for a blob client if the deployment needs it;
// endpoints only rely on IAlmacenamientoLogos.
public sealed class AlmacenamientoLogosLocales(IHostEnvironment environment, IConfiguration configuration) : IAlmacenamientoLogos
{
    private readonly string raiz = Path.GetFullPath(configuration["Storage:LogosPath"] is { Length: > 0 } configured
        ? (Path.IsPathRooted(configured) ? configured : Path.Combine(environment.ContentRootPath, configured))
        : Path.Combine(environment.ContentRootPath, "storage", "logos"));

    public async Task<LogoGuardado> GuardarAsync(int tenantId, string tipo, Stream contenido, string extension, CancellationToken ct)
    {
        var ruta = RutasLogo.Nuevo(tenantId, tipo, extension);
        var destino = Resolver(ruta);
        Directory.CreateDirectory(Path.GetDirectoryName(destino)!);
        await using (var archivo = new FileStream(destino, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await contenido.CopyToAsync(archivo, ct);
        }
        return new LogoGuardado(ruta, ContenidoPara(extension));
    }

    public Task<bool> ExisteAsync(string ruta, CancellationToken ct)
    {
        if (!RutasLogo.EsValido(ruta)) return Task.FromResult(false);
        return Task.FromResult(File.Exists(Resolver(ruta)));
    }

    public async Task<(Stream Contenido, string ContentType)?> AbrirAsync(string ruta, CancellationToken ct)
    {
        if (!RutasLogo.EsValido(ruta)) return null;
        var origen = Resolver(ruta);
        if (!File.Exists(origen)) return null;
        var archivo = new FileStream(origen, FileMode.Open, FileAccess.Read, FileShare.Read);
        try
        {
            return (archivo, ContenidoPara(Path.GetExtension(origen)));
        }
        catch
        {
            await archivo.DisposeAsync();
            throw;
        }
    }

    public async Task EliminarAsync(string? ruta, CancellationToken ct)
    {
        if (ruta is null || !RutasLogo.EsValido(ruta)) return;
        var origen = Resolver(ruta);
        if (!File.Exists(origen)) return;
        try
        {
            File.Delete(origen);
        }
        catch (IOException)
        {
            // A concurrent request may already be removing the same replaced logo.
        }
        await Task.CompletedTask;
    }

    private string Resolver(string ruta)
    {
        if (!RutasLogo.EsValido(ruta)) throw new ArgumentException("La ruta del logo no es segura.", nameof(ruta));
        var destino = Path.GetFullPath(Path.Combine(raiz, ruta));
        if (!destino.StartsWith(raiz + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException("La ruta del logo queda fuera del almacén.");
        return destino;
    }

    private static string ContenidoPara(string extension) => extension.ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".jpg" => "image/jpeg",
        ".webp" => "image/webp",
        _ => throw new ArgumentException("Extensión de imagen no admitida.", nameof(extension)),
    };
}
