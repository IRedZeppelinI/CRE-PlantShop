//using Microsoft.EntityFrameworkCore;
//using PlantShop.Domain.Entities.Shop;

//namespace PlantShop.Infrastructure.Persistence;

//public static class DataSeeder
//{
//    public static async Task SeedAsync(ApplicationDbContext context)
//    {
//        await context.Database.MigrateAsync();

//        if (await context.Categories.AnyAsync())
//        {
//            return; 
//        }

//        // Categorias
//        var catFlores = new Category { Name = "Flores",
//            Description = "Variedade de flores de interior e exterior." };

//        var catSuculentas = new Category { Name = "Suculentas",
//            Description = "Plantas resistentes com pouca necessidade de água." };

//        var catAcessorios = new Category { Name = "Acessórios",
//            Description = "Vasos, terra, fertilizantes e ferramentas." };

//        await context.Categories.AddRangeAsync(catFlores, catSuculentas, catAcessorios);
//        await context.SaveChangesAsync();


//        var articles = new List<Article>
//        {
//            // Flores
//            new Article { Name = "Rosa Vermelha (Vaso)",
//                Description = "Clássica rosa vermelha, ideal para oferecer.",
//                Price = 15.99m,
//                StockQuantity = 50,
//                CategoryId = catFlores.Id,
//                IsFeatured = true,
//                ImageUrl = "/images/stock/rosa_vermelha.jpg" },

//            new Article { Name = "Orquídea Phalaenopsis",
//                Description = "Orquídea elegante de fácil cuidado.",
//                Price = 24.90m,
//                StockQuantity = 30,
//                CategoryId = catFlores.Id,
//                IsFeatured = true,
//                ImageUrl = "/images/stock/orquidea_phalaenopsis.jpg" },

//            new Article { Name = "Lírio Branco",
//                Description = "Flor perfumada e sofisticada.",
//                Price = 12.50m,
//                StockQuantity = 40,
//                CategoryId = catFlores.Id,
//                ImageUrl = "/images/stock/lirio_branco.jpg"},

//            // Suculentas
//            new Article { Name = "Echeveria Elegans",
//                Description = "Suculenta em forma de roseta.",
//                Price = 5.99m,
//                StockQuantity = 100,
//                CategoryId = catSuculentas.Id,
//                ImageUrl = "/images/stock/echeveria_elegans.jpg" },

//            new Article { Name = "Sedum Morganianum (Dedo-de-moça)",
//                Description = "Suculenta pendente com folhas carnudas.",
//                Price = 8.50m,
//                StockQuantity = 60,
//                CategoryId = catSuculentas.Id,
//                IsFeatured = true,
//                ImageUrl = "/images/stock/sedum_morganianum.jpg" },

//            new Article { Name = "Aloe Vera",
//                Description = "Planta com propriedades medicinais.",
//                Price = 7.00m,
//                StockQuantity = 80,
//                CategoryId = catSuculentas.Id,
//                ImageUrl = "/images/stock/aloe_vera.jpg" },

//            // Acessórios
//            new Article { Name = "Vaso de Cerâmica (Pequeno)",
//                Description = "Vaso decorativo para plantas pequenas.",
//                Price = 4.50m,
//                StockQuantity = 150,
//                CategoryId = catAcessorios.Id,
//                ImageUrl = "/images/stock/vaso_ceramica_p.jpg"},

//            new Article { Name = "Substrato Universal (5L)",
//                Description = "Terra adequada para a maioria das plantas.",
//                Price = 3.99m,
//                StockQuantity = 200,
//                CategoryId = catAcessorios.Id,
//                ImageUrl = "/images/stock/substrato_universal.jpg" },

//            new Article { Name = "Fertilizante Líquido (250ml)",
//                Description = "Nutrientes essenciais para o crescimento.",
//                Price = 6.20m,
//                StockQuantity = 120,
//                CategoryId = catAcessorios.Id,
//                ImageUrl = "/images/stock/fertilizante_liquido.jpg" }
//        };

//        await context.Articles.AddRangeAsync(articles);

//        await context.SaveChangesAsync();
//    }
//}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlantShop.Domain.Entities;
using PlantShop.Domain.Entities.Shop;
using System.Diagnostics; 

namespace PlantShop.Infrastructure.Persistence;

public static class DataSeeder
{    
    public static async Task SeedAsync(
        ApplicationDbContext context,
        RoleManager<IdentityRole> roleManager,
        UserManager<AppUser> userManager)
    {
        
        await context.Database.MigrateAsync();
        Debug.WriteLine("Database migrations checked/applied.");
                
        await SeedRolesAsync(roleManager);
        await SeedAdminUserAsync(userManager);
        await SeedShopDataAsync(context);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
            Debug.WriteLine("Role 'Admin' created.");
        }
                
        if (!await roleManager.RoleExistsAsync("Customer"))
        {
            await roleManager.CreateAsync(new IdentityRole("Customer"));
            Debug.WriteLine("Role 'Customer' created.");
        }
    }

    private static async Task SeedAdminUserAsync(UserManager<AppUser> userManager)
    {
        const string adminEmail = "admin@plantshop.com";
        const string adminPass = "Admin123!"; 

        
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var adminUser = new AppUser
            {
                FullName = "Administrador",
                UserName = "admin", 
                Email = adminEmail,
                EmailConfirmed = true 
            };

            var result = await userManager.CreateAsync(adminUser, adminPass);

            if (result.Succeeded)
            {                
                await userManager.AddToRoleAsync(adminUser, "Admin");
                Debug.WriteLine("Admin user created and assigned to 'Admin' role.");
            }
            else
            {
                Debug.WriteLine("[ERROR] Could not create admin user:");
                foreach (var error in result.Errors)
                {
                    Debug.WriteLine($"- {error.Description}");
                }
            }
        }
    }

    private static async Task SeedShopDataAsync(ApplicationDbContext context)
    {        
        if (await context.Categories.AnyAsync())
        {
            Debug.WriteLine("Shop data (Categories/Articles) already exists. Skipping seed.");
            return;
        }

        Debug.WriteLine("Seeding Categories and Articles...");

        
        var catFlores = new Category
        {
            Name = "Flores",
            Description = "Variedade de flores de interior e exterior."
        };

        var catSuculentas = new Category
        {
            Name = "Suculentas",
            Description = "Plantas resistentes com pouca necessidade de água."
        };

        var catAcessorios = new Category
        {
            Name = "Acessórios",
            Description = "Vasos, terra, fertilizantes e ferramentas."
        };

        await context.Categories.AddRangeAsync(catFlores, catSuculentas, catAcessorios);
        await context.SaveChangesAsync();

        
        var articles = new List<Article>
        {
            new Article { Name = "Rosa Vermelha (Vaso)",
                Description = "Clássica rosa vermelha, ideal para oferecer.",
                Price = 15.99m,
                StockQuantity = 50,
                CategoryId = catFlores.Id,
                IsFeatured = true,
                ImageUrl = "/images/stock/rosa_vermelha.jpg" },

            new Article { Name = "Orquídea Phalaenopsis",
                Description = "Orquídea elegante de fácil cuidado.",
                Price = 24.90m,
                StockQuantity = 30,
                CategoryId = catFlores.Id,
                IsFeatured = true,
                ImageUrl = "/images/stock/orquidea_phalaenopsis.jpg" },

                        
            new Article { Name = "Lírio Branco",
                Description = "Flor perfumada e sofisticada.",
                Price = 12.50m,
                StockQuantity = 40,
                CategoryId = catFlores.Id,
                ImageUrl = "/images/stock/lirio_branco.jpg"},

            new Article { Name = "Echeveria Elegans",
                Description = "Suculenta em forma de roseta.",
                Price = 5.99m,
                StockQuantity = 100,
                CategoryId = catSuculentas.Id,
                ImageUrl = "/images/stock/echeveria_elegans.jpg" },

            new Article { Name = "Sedum Morganianum (Dedo-de-moça)",
                Description = "Suculenta pendente com folhas carnudas.",
                Price = 8.50m,
                StockQuantity = 60,
                CategoryId = catSuculentas.Id,
                IsFeatured = true,
                ImageUrl = "/images/stock/sedum_morganianum.jpg" },

            new Article { Name = "Aloe Vera",
                Description = "Planta com propriedades medicinais.",
                Price = 7.00m,
                StockQuantity = 80,
                CategoryId = catSuculentas.Id,
                ImageUrl = "/images/stock/aloe_vera.jpg" },

            new Article { Name = "Vaso de Cerâmica (Pequeno)",
                Description = "Vaso decorativo para plantas pequenas.",
                Price = 4.50m,
                StockQuantity = 150,
                CategoryId = catAcessorios.Id,
                ImageUrl = "/images/stock/vaso_ceramica_p.jpg"},

            new Article { Name = "Substrato Universal (5L)",
                Description = "Terra adequada para a maioria das plantas.",
                Price = 3.99m,
                StockQuantity = 200,
                CategoryId = catAcessorios.Id,
                ImageUrl = "/images/stock/substrato_universal.jpg" },

            new Article { Name = "Fertilizante Líquido (250ml)",
                Description = "Nutrientes essenciais para o crescimento.",
                Price = 6.20m,
                StockQuantity = 120,
                CategoryId = catAcessorios.Id,
                ImageUrl = "/images/stock/fertilizante_liquido.jpg" }
        };

        await context.Articles.AddRangeAsync(articles);
        await context.SaveChangesAsync();
        Debug.WriteLine("Shop data seeding complete.");
    }
}