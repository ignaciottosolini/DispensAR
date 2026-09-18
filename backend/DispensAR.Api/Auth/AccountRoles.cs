using Microsoft.AspNetCore.Identity;

namespace DispensAR.Api.Auth;

// Local platform maintenance only. No public endpoint may assign roles.
public static class AccountRoles
{
    public static TenantRole ReadRole()
    {
        Console.Write("Rol (0: UsuarioTenant / 1: Administrativo / 2: Administrador): ");
        var value = Console.ReadLine()?.Trim();
        if (!int.TryParse(value, out var number) || !Enum.IsDefined(typeof(TenantRole), number))
            throw new InvalidOperationException("El rol debe ser 0, 1 o 2.");
        return (TenantRole)number;
    }

    public static async Task AssignAsync(IServiceProvider services)
    {
        Console.Write("Email de la cuenta existente: ");
        var email = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(email)) throw new InvalidOperationException("Indica un email.");
        var users = services.GetRequiredService<UserManager<Account>>();
        var account = await users.FindByEmailAsync(email) ?? throw new InvalidOperationException("La cuenta no existe.");
        Console.WriteLine($"Cuenta: {account.Email} | Tenant: {account.TenantId} | Rol actual: {account.Rol}");
        var role = ReadRole();
        account.Rol = role;
        // Save role and rotate stamp in the same update to invalidate previous sessions.
        var result = await users.UpdateSecurityStampAsync(account);
        if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
        Console.WriteLine("Rol actualizado. La cuenta debe volver a iniciar sesion.");
    }
}
