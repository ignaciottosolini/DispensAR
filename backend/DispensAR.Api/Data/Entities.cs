namespace DispensAR.Api.Data;

public sealed class Organization
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = "";
}

public interface ITenantEntity
{
    int TenantId { get; set; }
}

public sealed class Branch : ITenantEntity
{
    public int TenantId { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
}

// Organization-scoped profile, not an authentication credential or login account.
public sealed class OrganizationUser : ITenantEntity
{
    public int TenantId { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DisplayName { get; set; } = "";
    public string Email { get; set; } = "";
}

public enum PacienteEstado { Activo = 0, Inactivo = 1 }

// Logical deletion: Inactivo preserves the row, the unique DNI and the future dispensations.
public sealed class Paciente : ITenantEntity
{
    public int TenantId { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nombres { get; set; } = "";
    public string Apellidos { get; set; } = "";
    public string Dni { get; set; } = "";
    public DateOnly? FechaNacimiento { get; set; }
    public PacienteEstado Estado { get; set; } = PacienteEstado.Activo;
    public DateOnly? AutorizacionHasta { get; set; }
    public string? Observaciones { get; set; }
    public byte[] Version { get; set; } = [];
}

public enum ProductoUnidad { gramos = 0, mililitros = 1, unidades = 2 }

// Per-organization product catalog. Units are never summed across products.
public sealed class Producto : ITenantEntity
{
    public int TenantId { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Codigo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public ProductoUnidad Unidad { get; set; } = ProductoUnidad.gramos;
    public bool Activo { get; set; } = true;
    public byte[] Version { get; set; } = [];
}

public sealed class Lote : ITenantEntity
{
    public int TenantId { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductoId { get; set; }
    public string Codigo { get; set; } = "";
    public DateOnly? FechaVencimiento { get; set; }
    public bool Activo { get; set; } = true;
    public byte[] Version { get; set; } = [];
}

