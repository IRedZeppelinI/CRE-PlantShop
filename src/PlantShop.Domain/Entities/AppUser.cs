using Microsoft.AspNetCore.Identity;
using PlantShop.Domain.Entities.Shop;

namespace PlantShop.Domain.Entities;

public class AppUser : IdentityUser
{

    public string FullName { get; set; } = string.Empty;
    public string? Address { get; set; } 

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
