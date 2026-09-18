using System.Text.RegularExpressions;
using DispensAR.Api.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DispensAR.Api.Pacientes;

public record PacienteInput(string? Nombres, string? Apellidos, string? Dni, DateOnly? FechaNacimiento,
    string? Estado, DateOnly? AutorizacionHasta, string? Observaciones, string? Version);
public record PacienteView(Guid Id, string Nombres, string Apellidos, string Dni, DateOnly? FechaNacimiento,
    string Estado, DateOnly? AutorizacionHasta, string? Observaciones, string Version);

public static class PacienteEndpoints
{
    public static void MapPacientes(this RouteGroupBuilder api)
    {
        api.MapGet("/pacientes", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok((await db.Pacientes.AsNoTracking().OrderBy(x => x.Apellidos).ThenBy(x => x.Nombres)
                .ToArrayAsync(ct)).Select(Viewed).ToArray()));
        api.MapGet("/pacientes/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var paciente = await db.Pacientes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
            return paciente is null ? Results.NotFound() : Results.Ok(Viewed(paciente));
        });
        api.MapPost("/pacientes", async (PacienteInput input, AppDbContext db, CancellationToken ct) =>
        {
            var (paciente, error) = ToNew(input);
            if (error is not null) return Results.Problem(error, statusCode: 400);
            db.Pacientes.Add(paciente!);
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateException exception) when (IsDuplicate(exception))
            { return Results.Conflict(new { detail = "Ya existe un paciente con ese DNI en tu asociación, incluso si está inactivo." }); }
            return Results.Created($"/api/pacientes/{paciente!.Id}", Viewed(paciente));
        }).RequireAuthorization("TenantOperations");
        api.MapPut("/pacientes/{id:guid}", async (Guid id, PacienteInput input, AppDbContext db, CancellationToken ct) =>
        {
            var error = Validate(input, requireDni: false);
            if (error is not null) return Results.Problem(error, statusCode: 400);
            var paciente = await db.Pacientes.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (paciente is null) return Results.NotFound();
            if (input.Dni is not null && input.Dni.Trim() != paciente.Dni)
                return Results.Problem("El DNI no se modifica: identificá al paciente por su DNI original.", statusCode: 400);
            if (Convert.ToBase64String(paciente.Version) != input.Version)
                return Results.Conflict(new { detail = "El paciente cambió. Recargá antes de guardar." });
            Apply(paciente, input, applyDni: false);
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateConcurrencyException)
            { return Results.Conflict(new { detail = "El paciente cambió. Recargá antes de guardar." }); }
            return Results.Ok(Viewed(paciente));
        }).RequireAuthorization("TenantOperations");
        api.MapDelete("/pacientes/{id:guid}", async (Guid id, HttpRequest request, AppDbContext db, CancellationToken ct) =>
        {
            var paciente = await db.Pacientes.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (paciente is null) return Results.NotFound();
            var version = request.Headers.IfMatch.ToString().Trim('"');
            if (Convert.ToBase64String(paciente.Version) != version)
                return Results.Conflict(new { detail = "El paciente cambió. Recargá antes de continuar." });
            // Logical deletion: the row, its DNI reservation and future dispensations stay.
            paciente.Estado = PacienteEstado.Inactivo;
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateConcurrencyException)
            { return Results.Conflict(new { detail = "El paciente cambió. Recargá antes de continuar." }); }
            return Results.NoContent();
        }).RequireAuthorization("TenantOperations");
    }

    private static bool IsDuplicate(DbUpdateException exception) => exception.InnerException is SqlException { Number: 2601 or 2627 };
    private static PacienteView Viewed(Paciente paciente) => new(paciente.Id, paciente.Nombres, paciente.Apellidos,
        paciente.Dni, paciente.FechaNacimiento, paciente.Estado.ToString(), paciente.AutorizacionHasta,
        paciente.Observaciones, Convert.ToBase64String(paciente.Version));

    private static (Paciente?, string?) ToNew(PacienteInput input)
    {
        var error = Validate(input, requireDni: true);
        if (error is not null) return (null, error);
        var paciente = new Paciente();
        Apply(paciente, input, applyDni: true);
        return (paciente, null);
    }

    private static void Apply(Paciente paciente, PacienteInput input, bool applyDni)
    {
        paciente.Nombres = input.Nombres!.Trim();
        paciente.Apellidos = input.Apellidos!.Trim();
        if (applyDni) paciente.Dni = input.Dni!.Trim();
        paciente.FechaNacimiento = input.FechaNacimiento;
        if (input.Estado is not null)
            paciente.Estado = input.Estado.Trim().ToLowerInvariant() == "inactivo" ? PacienteEstado.Inactivo : PacienteEstado.Activo;
        else if (applyDni) paciente.Estado = PacienteEstado.Activo;
        paciente.AutorizacionHasta = input.AutorizacionHasta;
        paciente.Observaciones = string.IsNullOrWhiteSpace(input.Observaciones) ? null : input.Observaciones.Trim();
    }

    private static string? Validate(PacienteInput input, bool requireDni)
    {
        if (string.IsNullOrWhiteSpace(input.Nombres) || input.Nombres.Trim().Length > 160)
            return "Los nombres deben tener entre 1 y 160 caracteres.";
        if (string.IsNullOrWhiteSpace(input.Apellidos) || input.Apellidos.Trim().Length > 160)
            return "Los apellidos deben tener entre 1 y 160 caracteres.";
        if (requireDni && !Regex.IsMatch(input.Dni?.Trim() ?? "", "^[0-9]{6,10}$", RegexOptions.CultureInvariant))
            return "El DNI debe tener entre 6 y 10 dígitos, sin puntos ni espacios.";
        if (input.Estado is not null && input.Estado.Trim().ToLowerInvariant() is not ("activo" or "inactivo"))
            return "El estado debe ser activo o inactivo.";
        // Buffer so a local date that is already tomorrow in UTC is not rejected.
        if (input.FechaNacimiento > DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)))
            return "La fecha de nacimiento no puede ser futura.";
        if (input.Observaciones is not null && input.Observaciones.Trim().Length > 1000)
            return "Las observaciones no pueden superar los 1000 caracteres.";
        return null;
    }
}
