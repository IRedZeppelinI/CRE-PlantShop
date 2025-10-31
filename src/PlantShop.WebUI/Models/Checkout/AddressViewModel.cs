using System.ComponentModel.DataAnnotations;

namespace PlantShop.WebUI.Models.Checkout;

public class AddressViewModel
{
    [Required(ErrorMessage = "A morada de envio é obrigatória.")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "A morada deve ter entre 10 e 500 caracteres.")]
    [Display(Name = "Morada de Envio")]
    public string Address { get; set; } = string.Empty;
}