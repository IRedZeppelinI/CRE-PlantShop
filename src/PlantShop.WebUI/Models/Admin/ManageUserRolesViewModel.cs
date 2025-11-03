using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace PlantShop.WebUI.Models.Admin;

// Helper class para a checkbox
public class RoleCheckboxViewModel
{
    public string RoleName { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}

public class ManageUserRolesViewModel
{
    [HiddenInput]
    public string UserId { get; set; } = string.Empty;

    [Display(Name = "Utilizador")]
    public string UserName { get; set; } = string.Empty;

    public List<RoleCheckboxViewModel> Roles { get; set; } = new List<RoleCheckboxViewModel>();
}