namespace DispensAR.Api.Tenancy;

public sealed class TenantContext
{
    private int? tenantId;
    public int Id => tenantId ?? throw new InvalidOperationException("No hay un tenant seleccionado.");

    public void Select(int id)
    {
        if (tenantId is not null)
            throw new InvalidOperationException("El tenant no puede cambiar dentro de una solicitud.");
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        tenantId = id;
    }
}

