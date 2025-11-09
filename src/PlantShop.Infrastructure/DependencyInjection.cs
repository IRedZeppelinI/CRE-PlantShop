using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlantShop.Application.Interfaces.Infrastructure;
using PlantShop.Application.Interfaces.Persistence;
using PlantShop.Application.Interfaces.Persistence.Cosmos;
using PlantShop.Domain.Entities;
using PlantShop.Infrastructure.Persistence;
using PlantShop.Infrastructure.Persistence.Repositories.Cosmos;
using PlantShop.Infrastructure.Services;

namespace PlantShop.Infrastructure;


public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var DbConnectionString = configuration.GetConnectionString("DefaultConnection");
        var StorageAccountConnectionString = configuration.GetConnectionString("StorageAccount");
        var ServiceBusConnectionString = configuration.GetConnectionString("ServiceBus");
        var CosmosDbConnectionString = configuration.GetConnectionString("CosmosDb");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(DbConnectionString));

        

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


        //Azure
        //Blobs        
        services.AddSingleton(serviceProvider => new BlobServiceClient(StorageAccountConnectionString));
        services.AddScoped<IFileStorageService, BlobStorageService>();

        //bus queue
        services.AddSingleton(x => new ServiceBusClient(ServiceBusConnectionString));
        services.AddScoped<IMessagePublisher, ServiceBusPublisher>();

        //cosmosDB
        services.AddSingleton(x =>
        {            
            // Serializer para gravar como camelCase
            var cosmosClientOptions = new CosmosClientOptions
            {
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            };
            return new CosmosClient(CosmosDbConnectionString, cosmosClientOptions);
        });

        services.AddSingleton<CosmosDbContext>();

        services.AddScoped<IDailyChallengeRepository, DailyChallengeRepository>();
        services.AddScoped<ICommunityPostRepository, CommunityPostRepository>();

        return services;
    }
}