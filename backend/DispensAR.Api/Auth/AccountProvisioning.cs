using DispensAR.Api.Data;
using DispensAR.Api.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace DispensAR.Api.Auth;

public static class AccountProvisioning
{
    public static async Task CreateAsync(IServiceProvider services)
    {
        Console.Write("Numero de tenant (1: Demo / 2: ACME): ");
        var tenantInput = Console.ReadLine()?.Trim();
        if (!int.TryParse(tenantInput, out var tenantId) || tenantId <= 0) throw new InvalidOperationException("Indica un tenant.");
        services.GetRequiredService<TenantContext>().Select(tenantId);
        var db = services.GetRequiredService<AppDbContext>();
        if (!await db.Organizations.AnyAsync()) throw new InvalidOperationException("El tenant no existe. Aplica las migraciones primero.");
        Console.Write("Email: ");
        var email = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(email)) throw new InvalidOperationException("Indica un email.");
        var users = services.GetRequiredService<UserManager<Account>>();
        if (await users.FindByEmailAsync(email) is not null)
            throw new InvalidOperationException("El email ya tiene una cuenta. No se modifico la cuenta existente.");
        var role = AccountRoles.ReadRole();
        Console.WriteLine("Contrasena: minimo 12 caracteres, mayuscula, minuscula, numero y simbolo.");
        Console.Write("Contrasena (oculta): ");
        var password = ReadPassword();
        Console.Write("Repetir contrasena: ");
        if (password != ReadPassword()) throw new InvalidOperationException("Las contrasenas no coinciden.");
        var result = await users.CreateAsync(new Account { TenantId = tenantId, Email = email, UserName = email, Rol = role }, password);
        if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
        Console.WriteLine("Cuenta creada. Ya podes iniciar sesion.");
    }

    private static string ReadPassword()
    {
        if (Console.IsInputRedirected) throw new InvalidOperationException("Usa una terminal interactiva para ingresar la contrasena.");
        var buffer = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); return buffer.ToString(); }
            if (key.Key == ConsoleKey.Backspace) { if (buffer.Length > 0) buffer.Length--; }
            else if (!char.IsControl(key.KeyChar) && buffer.Length < 1024) buffer.Append(key.KeyChar);
        }
    }
}

