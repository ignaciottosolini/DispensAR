using DispensAR.Api.Data;

namespace DispensAR.Api.Dashboard;

public sealed class DashboardWidget
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string Tamano { get; set; } = "normal";
    public string TipoIndicador { get; set; } = "";
    public bool Activo { get; set; } = true;
    public byte[] Version { get; set; } = [];
}

public sealed class OrganizationWidget : ITenantEntity
{
    public int TenantId { get; set; }
    public int WidgetId { get; set; }
    public bool Habilitado { get; set; }
    public int Orden { get; set; }
    public byte[] Version { get; set; } = [];
}

public record WidgetConfiguration(int Id, string Codigo, string Descripcion, string Tamano,
    bool Habilitado, int Orden, string? Version);
public record WidgetChange(int Id, bool Habilitado, int Orden, string? Version);
public record DashboardChanges(WidgetChange[]? Widgets);

public static class DashboardCatalog
{
    public static DashboardWidget[] Widgets =>
    [
        new() { Id = 1, Codigo = "pacientes_activos", TipoIndicador = "pacientes_activos", Descripcion = "Pacientes activos" },
        new() { Id = 2, Codigo = "cantidad_dispensada", TipoIndicador = "cantidad_dispensada", Descripcion = "Cantidad dispensada" },
        new() { Id = 3, Codigo = "stock_disponible", TipoIndicador = "stock_disponible", Descripcion = "Stock disponible" },
        new() { Id = 4, Codigo = "sucursales", TipoIndicador = "sucursales", Descripcion = "Sucursales" },
        new() { Id = 5, Codigo = "alertas", TipoIndicador = "alertas", Descripcion = "Alertas", Tamano = "ancho" },
        new() { Id = 6, Codigo = "ultimas_dispensaciones", TipoIndicador = "ultimas_dispensaciones", Descripcion = "Últimas dispensaciones", Tamano = "ancho" }
    ];
}

