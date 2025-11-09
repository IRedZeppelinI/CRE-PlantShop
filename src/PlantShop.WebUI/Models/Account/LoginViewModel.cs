using System.ComponentModel.DataAnnotations;

namespace PlantShop.WebUI.Models.Account;

public class LoginViewModel
{
    [Required(ErrorMessage = "O email é obrigatório.")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A password é obrigatória.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Lembrar-me?")]
    public bool RememberMe { get; set; }

    
    // para  redirecionar após o login
    public string? ReturnUrl { get; set; }
}