namespace PlantShop.Domain.Entities.Shop;

public class Order
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    
    public AppUser User { get; set; } = null!; //Para não usar User User

    public DateTime OrderDate { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}