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

    // Este campo será preenchido pelo sistema (query string) 
    // para podermos redirecionar o utilizador de volta após o login.
    public string? ReturnUrl { get; set; }
}