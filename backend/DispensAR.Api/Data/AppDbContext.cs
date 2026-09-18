using DispensAR.Api.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using DispensAR.Api.Auth;
using Microsoft.AspNetCore.Identity;
using DispensAR.Api.Dashboard;
using DispensAR.Api.Identidad;

namespace DispensAR.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, TenantContext tenant) : IdentityUserContext<Account>(options)
{
    public int CurrentTenantId => tenant.Id;
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<OrganizationUser> Profiles => Set<OrganizationUser>();
    public DbSet<DashboardWidget> DashboardWidgets => Set<DashboardWidget>();
    public DbSet<OrganizationWidget> OrganizationWidgets => Set<OrganizationWidget>();
    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Lote> Lotes => Set<Lote>();
    public DbSet<IdentidadVisual> IdentidadesVisuales => Set<IdentidadVisual>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);
        model.Entity<DashboardWidget>(entity =>
        {
            entity.ToTable("WidgetsDashboard");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Codigo).HasMaxLength(80).IsRequired();
            entity.HasIndex(x => x.Codigo).IsUnique();
            entity.Property(x => x.Descripcion).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Tamano).HasMaxLength(20).IsRequired();
            entity.Property(x => x.TipoIndicador).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Activo).HasDefaultValue(true);
            entity.Property(x => x.Version).IsRowVersion();
            entity.HasData(DashboardCatalog.Widgets);
        });
        model.Entity<OrganizationWidget>(entity =>
        {
            entity.ToTable("WidgetsDashboardOrganizaciones");
            entity.HasKey(x => new { x.TenantId, x.WidgetId });
            entity.Property(x => x.Version).IsRowVersion();
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<DashboardWidget>().WithMany().HasForeignKey(x => x.WidgetId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => x.TenantId == CurrentTenantId);
            entity.HasData(DashboardCatalog.Widgets.SelectMany(widget => new[] { 1, 2 }.Select(tenantId =>
                new OrganizationWidget { TenantId = tenantId, WidgetId = widget.Id, Orden = widget.Id - 1,
                    Habilitado = widget.Id <= 4 && !(tenantId == 2 && widget.Id == 3) })));
        });
        model.Entity<IdentityUserClaim<string>>().ToTable("AtributosUsuarios");
        model.Entity<IdentityUserLogin<string>>().ToTable("AccesosExternosUsuarios");
        model.Entity<IdentityUserToken<string>>().ToTable("TokensUsuarios");
        model.HasSequence<int>("TenantNumbers").StartsAt(3);
        model.Entity<IdentityUserLogin<string>>().Property(x => x.LoginProvider).HasMaxLength(128);
        model.Entity<IdentityUserLogin<string>>().Property(x => x.ProviderKey).HasMaxLength(128);
        model.Entity<IdentityUserToken<string>>().Property(x => x.LoginProvider).HasMaxLength(128);
        model.Entity<IdentityUserToken<string>>().Property(x => x.Name).HasMaxLength(128);
        model.Entity<Account>(entity =>
        {
            entity.ToTable("Usuarios");
            entity.Property(x => x.TenantId).IsRequired();
            entity.Property(x => x.Email).IsRequired();
            entity.Property(x => x.NormalizedEmail).IsRequired();
            entity.HasIndex(x => x.NormalizedEmail).HasDatabaseName("EmailIndex").IsUnique();
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<Organization>(entity =>
        {
            entity.ToTable("Organizaciones");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasDefaultValueSql("NEXT VALUE FOR [TenantNumbers]");
            entity.Property(x => x.Descripcion).HasMaxLength(160).IsRequired();
            entity.HasQueryFilter(x => x.Id == CurrentTenantId);
            entity.HasData(new Organization { Id = 1, Descripcion = "DispensAR Demo" },
                new Organization { Id = 2, Descripcion = "ACME" });
        });
        model.Entity<Branch>(entity =>
        {
            entity.ToTable("Sucursales");
            // Composite key also scopes UPDATE/DELETE predicates to the tenant.
            entity.HasKey(x => new { x.TenantId, x.Id });
            entity.Property(x => x.TenantId).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => x.TenantId == CurrentTenantId);
            entity.HasData(
                new Branch { TenantId = 1, Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), Name = "Sucursal Centro" },
                new Branch { TenantId = 1, Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), Name = "Sucursal Norte" },
                new Branch { TenantId = 2, Id = Guid.Parse("20000000-0000-0000-0000-000000000001"), Name = "Sucursal Sur" });
        });
        model.Entity<OrganizationUser>(entity =>
        {
            entity.ToTable("PerfilesUsuarios"); // Profiles are separate from login accounts.
            entity.HasKey(x => new { x.TenantId, x.Id });
            entity.Property(x => x.TenantId).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(254).IsRequired();
            entity.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => x.TenantId == CurrentTenantId);
            entity.HasData(
                new OrganizationUser { TenantId = 1, Id = Guid.Parse("30000000-0000-0000-0000-000000000001"), DisplayName = "Usuario Demo", Email = "demo@example.test" },
                new OrganizationUser { TenantId = 2, Id = Guid.Parse("40000000-0000-0000-0000-000000000001"), DisplayName = "Usuario ACME", Email = "acme@example.test" });
        });
        model.Entity<Paciente>(entity =>
        {
            entity.ToTable("Pacientes");
            // Composite key also scopes UPDATE/DELETE predicates to the tenant.
            entity.HasKey(x => new { x.TenantId, x.Id });
            entity.Property(x => x.TenantId).IsRequired();
            entity.Property(x => x.Nombres).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Apellidos).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Dni).HasMaxLength(10).IsFixedLength(false).IsRequired();
            // Deactivated patients keep occupying their DNI, like catalog codes after logical deletion.
            entity.HasIndex(x => new { x.TenantId, x.Dni }).IsUnique();
            entity.Property(x => x.Estado).HasConversion<int>().HasDefaultValue(PacienteEstado.Activo);
            entity.Property(x => x.Observaciones).HasMaxLength(1000);
            entity.Property(x => x.Version).IsRowVersion();
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => x.TenantId == CurrentTenantId);
        });
        model.Entity<Producto>(entity =>
        {
            entity.ToTable("Productos");
            entity.HasKey(x => new { x.TenantId, x.Id });
            entity.Property(x => x.TenantId).IsRequired();
            entity.Property(x => x.Codigo).HasMaxLength(80).IsRequired();
            entity.HasIndex(x => new { x.TenantId, x.Codigo }).IsUnique();
            entity.Property(x => x.Descripcion).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Unidad).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.Activo).HasDefaultValue(true);
            entity.Property(x => x.Version).IsRowVersion();
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => x.TenantId == CurrentTenantId);
        });
        model.Entity<Lote>(entity =>
        {
            entity.ToTable("Lotes");
            entity.HasKey(x => new { x.TenantId, x.Id });
            entity.Property(x => x.TenantId).IsRequired();
            entity.Property(x => x.Codigo).HasMaxLength(80).IsRequired();
            entity.HasIndex(x => new { x.TenantId, x.ProductoId, x.Codigo }).IsUnique();
            entity.Property(x => x.Activo).HasDefaultValue(true);
            entity.Property(x => x.Version).IsRowVersion();
            // The composite FK keeps every lot inside the same tenant as its product.
            entity.HasOne<Producto>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductoId }).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => x.TenantId == CurrentTenantId);
        });
        model.Entity<IdentidadVisual>(entity =>
        {
            entity.ToTable("IdentidadesVisuales");
            // One visual identity per organization: the tenant id is also the key.
            entity.HasKey(x => x.TenantId);
            entity.Property(x => x.TenantId).IsRequired();
            entity.Property(x => x.LogoPrincipalRuta).HasMaxLength(160);
            entity.Property(x => x.LogoPrincipalContenido).HasMaxLength(40);
            entity.Property(x => x.LogoCompactoRuta).HasMaxLength(160);
            entity.Property(x => x.LogoCompactoContenido).HasMaxLength(40);
            entity.Property(x => x.ColorPrimario).HasMaxLength(7);
            entity.Property(x => x.ColorSecundario).HasMaxLength(7);
            entity.Property(x => x.ColorSidebarFondo).HasMaxLength(7);
            entity.Property(x => x.ColorSidebarTexto).HasMaxLength(7);
            entity.Property(x => x.Version).IsRowVersion();
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => x.TenantId == CurrentTenantId);
        });
    }

    private void ValidateTenantWrites()
    {
        ChangeTracker.DetectChanges();
        foreach (var entry in ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            if (entry.Entity is Account && entry.State == EntityState.Modified &&
                !Equals(entry.Property(nameof(Account.TenantId)).OriginalValue,
                    entry.Property(nameof(Account.TenantId)).CurrentValue))
                throw new InvalidOperationException("Una cuenta no puede cambiar de tenant.");
            if (entry.Entity is Organization)
                throw new InvalidOperationException("Las organizaciones se administran mediante un flujo separado de provisionamiento.");
            if (entry.Entity is not ITenantEntity entity) continue;
            var id = CurrentTenantId;
            if (entry.State == EntityState.Added && entity.TenantId == 0)
                entity.TenantId = id;
            if (entity.TenantId != id || (entry.State != EntityState.Added &&
                !Equals(entry.Property(nameof(ITenantEntity.TenantId)).OriginalValue, id)))
                throw new InvalidOperationException("No se permite escribir datos de otro tenant.");
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidateTenantWrites();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ValidateTenantWrites();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}

