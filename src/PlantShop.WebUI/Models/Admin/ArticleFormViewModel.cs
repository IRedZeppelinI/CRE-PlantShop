using Microsoft.AspNetCore.Mvc.Rendering;
using PlantShop.Application.DTOs.Shop;
using System.ComponentModel.DataAnnotations;

namespace PlantShop.WebUI.Models.Admin;

public class ArticleFormViewModel
{
    public ArticleDto Article { get; set; } = new ArticleDto();

    // 2. Adiciona o campo para o upload do ficheiro
    [Display(Name = "Imagem do Artigo")]
    public IFormFile? ImageFile { get; set; }
}