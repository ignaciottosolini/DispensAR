using DispensAR.Api.Data;

namespace DispensAR.Api.Identidad;

// Visual identity of one organization: a single row per tenant. Colors are normalized
// #rrggbb values and logos are storage references, never base64 payloads.
public sealed class IdentidadVisual : ITenantEntity
{
    public int TenantId { get; set; }
    public string? LogoPrincipalRuta { get; set; }
    public string? LogoPrincipalContenido { get; set; }
    public string? LogoCompactoRuta { get; set; }
    public string? LogoCompactoContenido { get; set; }
    public string? ColorPrimario { get; set; }
    public string? ColorSecundario { get; set; }
    public string? ColorSidebarFondo { get; set; }
    public string? ColorSidebarTexto { get; set; }
    public byte[] Version { get; set; } = [];
}

public record IdentidadView(string Descripcion, IdentidadVisualView? Identidad);

public record IdentidadVisualView(string? LogoPrincipalUrl, string? LogoCompactoUrl, string? ColorPrimario,
    string? ColorSecundario, string? ColorSidebarFondo, string? ColorSidebarTexto, string Version);
public record IdentidadInput(string? LogoPrincipalRuta, string? LogoCompactoRuta, string? ColorPrimario,
    string? ColorSecundario, string? ColorSidebarFondo, string? ColorSidebarTexto, string? Version);
