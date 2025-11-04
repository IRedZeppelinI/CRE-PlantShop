using Microsoft.Extensions.DependencyInjection;
using PlantShop.Application.Interfaces.Services.Community;
using PlantShop.Application.Interfaces.Services.Shop;
using PlantShop.Application.Services.Shop;

namespace PlantShop.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IArticleService, ArticleService>();
        services.AddScoped<IOrderService, OrderService>();

        //services.AddScoped<ICommunityService, CommunityService>();

        return services;
    }
}
