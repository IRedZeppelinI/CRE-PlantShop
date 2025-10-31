using PlantShop.WebUI.Models.Cart;

namespace PlantShop.WebUI.Models.Cart
{
    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new List<CartItemViewModel>();
                
        public decimal Subtotal => Items.Sum(item => item.TotalPrice);
                
        public decimal ShippingCost { get; set; } = 0;
                
        public decimal Total => Subtotal + ShippingCost;
    }
}