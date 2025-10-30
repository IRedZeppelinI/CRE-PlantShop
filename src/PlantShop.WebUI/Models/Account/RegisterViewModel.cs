using System.ComponentModel.DataAnnotations;

namespace PlantShop.WebUI.Models.Account;

public class RegisterViewModel
{
    [Required(ErrorMessage = "O nome completo é obrigatório.")]
    [Display(Name = "Nome Completo")]
    public string FullName { get; set; } = string.Empty;

    
    [Required(ErrorMessage = "O username é obrigatório.")]
    [StringLength(100, ErrorMessage = "O {0} deve ter entre {2} e {1} caracteres.", MinimumLength = 3)]
    [RegularExpression(@"^[a-zA-Z0-9_.-]*$", ErrorMessage = "O username só pode conter letras, números, underscore (_), ponto (.) ou hífen (-).")]
    [Display(Name = "Username")]
    public string UserName { get; set; } = string.Empty;
    

    [Required(ErrorMessage = "O email é obrigatório.")]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A password é obrigatória.")]
    [StringLength(100, ErrorMessage = "A {0} deve ter pelo menos {2} caracteres.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirmar password")]
    [Compare("Password", ErrorMessage = "A password e a confirmação não correspondem.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}