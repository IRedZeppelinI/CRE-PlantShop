using PlantShop.Application.DTOs.Community;
using System.ComponentModel.DataAnnotations;

namespace PlantShop.WebUI.Models.Community;

public class PostDetailsViewModel
{
    public CommunityPostDto Post { get; set; } = null!;
        
    [Required(ErrorMessage = "O comentário não pode estar vazio.")]
    [StringLength(500, ErrorMessage = "O comentário não pode exceder 500 caracteres.")]
    [Display(Name = "O seu Comentário")]
    public string NewCommentText { get; set; } = string.Empty;
}