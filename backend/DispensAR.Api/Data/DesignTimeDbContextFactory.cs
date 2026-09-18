using DispensAR.Api.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DispensAR.Api.Data;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Model generation needs no server or credentials. Apply with --migrate instead.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=tcp:127.0.0.1,1433;Database=DispensAR;Integrated Security=True;TrustServerCertificate=True")
            .Options;
        return new AppDbContext(options, new TenantContext());
    }
}
