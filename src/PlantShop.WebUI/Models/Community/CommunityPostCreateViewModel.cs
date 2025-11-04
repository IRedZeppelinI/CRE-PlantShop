using System.ComponentModel.DataAnnotations;

namespace PlantShop.WebUI.Models.Community;

public class CommunityPostCreateViewModel
{
    [Required(ErrorMessage = "O título é obrigatório.")]
    [StringLength(100, MinimumLength = 5, ErrorMessage = "O título deve ter entre 5 e 100 caracteres.")]
    [Display(Name = "Título do seu pedido")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Descrição")]
    [StringLength(1000, ErrorMessage = "A descrição não pode exceder 1000 caracteres.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Por favor, carregue uma foto da planta.")]
    [Display(Name = "Foto da Planta")]
    public IFormFile ImageFile { get; set; } = null!;
}