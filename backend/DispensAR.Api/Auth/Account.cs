using Microsoft.AspNetCore.Identity;

namespace DispensAR.Api.Auth;

public sealed class Account : IdentityUser
{
    public int TenantId { get; set; }
    public TenantRole Rol { get; set; } = TenantRole.UsuarioTenant;
    public bool EsAdministradorPlataforma { get; set; }
}

public enum TenantRole { UsuarioTenant = 0, Administrativo = 1, Administrador = 2 }

