using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Identity;
using PlantShop.Application;
using PlantShop.Domain.Entities;
using PlantShop.Infrastructure;
using PlantShop.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

//configurar injeção de dependencias de application e infrastructure
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();

// Add services to the container.
builder.Services.AddControllersWithViews();

//cookie de sessão
builder.Services.AddDistributedMemoryCache(); 
builder.Services.AddSession(options =>
{    
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

//seed da database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var blobClient = services.GetRequiredService<BlobServiceClient>();

        await DataSeeder.SeedAsync(context, roleManager, userManager, blobClient);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
        throw; 
    }
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var cosmosContext = services.GetRequiredService<CosmosDbContext>();
        await cosmosContext.InitializeDatabaseAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogCritical(ex, "An error occurred while initializing the CosmosDB database.");
        throw; 
    }
}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseStatusCodePagesWithReExecute("/Home/NotFoundPage");

app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
