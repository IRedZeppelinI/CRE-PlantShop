using PlantShop.Application.DTOs.Community;
using System.ComponentModel.DataAnnotations;

namespace PlantShop.WebUI.Models.Admin;

public class DailyChallengeFormViewModel
{    
    public DailyChallengeDto Challenge { get; set; } = new DailyChallengeDto();
        
    [Display(Name = "Imagem do Desafio")]
    public IFormFile? ImageFile { get; set; }
        
    [Display(Name = "Data do Desafio")]
    [DataType(DataType.Date)]
    public DateTime ChallengeDate { get; set; } = DateTime.UtcNow.Date;
}