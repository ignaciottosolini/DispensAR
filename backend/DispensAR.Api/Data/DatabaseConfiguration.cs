using Microsoft.Data.SqlClient;

namespace DispensAR.Api.Data;

public static class DatabaseConfiguration
{
    public static string ConnectionString(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration.GetConnectionString("DispensAR");
        if (!string.IsNullOrWhiteSpace(configured)) return configured;
        if (!environment.IsDevelopment())
            throw new InvalidOperationException("Configura ConnectionStrings__DispensAR para este ambiente.");

        // Reuse the existing local Docker password without copying it to source files.
        var directory = new DirectoryInfo(environment.ContentRootPath);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "compose.yaml")))
            directory = directory.Parent;
        var envFile = directory is null ? null : Path.Combine(directory.FullName, ".env");
        var password = Environment.GetEnvironmentVariable("MSSQL_SA_PASSWORD");
        if (string.IsNullOrWhiteSpace(password) && envFile is not null && File.Exists(envFile))
            password = File.ReadLines(envFile)
                .FirstOrDefault(line => line.StartsWith("MSSQL_SA_PASSWORD=", StringComparison.Ordinal))?
                ["MSSQL_SA_PASSWORD=".Length..].Trim();
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Falta .env: ejecuta scripts/start-db.ps1 o configura ConnectionStrings__DispensAR.");
        return new SqlConnectionStringBuilder
        {
            DataSource = "tcp:127.0.0.1,1433", InitialCatalog = "DispensAR",
            UserID = "sa", Password = password, Encrypt = true, TrustServerCertificate = true,
            PersistSecurityInfo = false
        }.ConnectionString;
    }
}
