using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;

internal class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ServiceCatalogDbContext>
{
    ServiceCatalogDbContext IDesignTimeDbContextFactory<ServiceCatalogDbContext>.CreateDbContext(string[] args)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var builder = new DbContextOptionsBuilder<ServiceCatalogDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        builder.UseNpgsql(connectionString);

        return new ServiceCatalogDbContext(builder.Options);
    }
}
