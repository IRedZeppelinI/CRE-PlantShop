using PlantShop.Application.DTOs.Shop;
using PlantShop.WebUI.Models.Checkout; 
using System.Collections.Generic;

namespace PlantShop.WebUI.Models.User;

public class ProfileViewModel
{
    // Para o formulário de edição da morada
    public AddressViewModel AddressInfo { get; set; } = new AddressViewModel();

    public IEnumerable<OrderDto> OrderHistory { get; set; } = new List<OrderDto>();

    public string FullName { get; set; } = string.Empty;
}