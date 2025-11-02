using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlantShop.Application.Interfaces.Infrastructure;
using PlantShop.Application.Interfaces.Persistence;
using PlantShop.Domain.Entities;
using PlantShop.Infrastructure.Persistence;
using PlantShop.Infrastructure.Services;

namespace PlantShop.Infrastructure;


public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var DbConnectionString = configuration.GetConnectionString("DefaultConnection");
        var StorageAccountConnectionString = configuration.GetConnectionString("StorageAccount");

        services.AddDbContext<ApplicationDbContext>(options =>
            //options.UseSqlServer(connectionString)); 
            options.UseNpgsql(DbConnectionString));

        services.AddSingleton(x =>
            new BlobServiceClient(StorageAccountConnectionString));

        services.AddIdentity<AppUser, IdentityRole>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 6;
        })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton(serviceProvider => new BlobServiceClient(StorageAccountConnectionString));
        services.AddScoped<IFileStorageService, BlobStorageService>();

        return services;
    }
}