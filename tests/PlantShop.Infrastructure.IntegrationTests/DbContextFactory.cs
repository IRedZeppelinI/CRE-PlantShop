using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PlantShop.Infrastructure.Persistence;
using System;

namespace PlantShop.Infrastructure.IntegrationTests;

public static class DbContextFactory
{
    public static ApplicationDbContext Create()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.IntegrationTests.json")
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()            
            .UseNpgsql(connectionString,
                b => b.MigrationsAssembly("PlantShop.Infrastructure")
            )
            .Options;

        var context = new ApplicationDbContext(options);

        context.Database.EnsureDeleted();
        context.Database.Migrate();

        return context;
    }
}
