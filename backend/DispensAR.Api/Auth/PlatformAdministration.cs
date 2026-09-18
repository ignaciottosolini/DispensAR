using Microsoft.AspNetCore.Identity;

namespace DispensAR.Api.Auth;

public static class PlatformAdministration
{
    public static async Task AssignAsync(IServiceProvider services)
    {
        Console.Write("Email de la cuenta: ");
        var email = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(email)) throw new InvalidOperationException("Indica un email.");
        var users = services.GetRequiredService<UserManager<Account>>();
        var account = await users.FindByEmailAsync(email) ?? throw new InvalidOperationException("La cuenta no existe.");
        Console.WriteLine($"Cuenta: {account.Email} | Tenant: {account.TenantId} | Administrador de plataforma: {account.EsAdministradorPlataforma}");
        Console.Write("Permiso global para administrar elementos de todas las asociaciones (1: otorgar / 0: revocar): ");
        var choice = Console.ReadLine()?.Trim();
        if (choice is not ("0" or "1")) throw new InvalidOperationException("Ingresa 0 o 1.");
        account.EsAdministradorPlataforma = choice == "1";
        var result = await users.UpdateSecurityStampAsync(account);
        if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
        Console.WriteLine("Permiso actualizado. Volve a iniciar sesion. El rol del tenant no fue modificado.");
    }
}
