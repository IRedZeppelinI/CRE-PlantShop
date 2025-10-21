using PlantShop.Domain.Entities.Shop;

namespace PlantShop.Domain.Entities;

public class User
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Address { get; set; } 

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
