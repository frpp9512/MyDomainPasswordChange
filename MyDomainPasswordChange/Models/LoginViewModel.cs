using System.ComponentModel.DataAnnotations;

namespace MyDomainPasswordChange.Models;

public class LoginViewModel
{
    [Required]
    [Display(Name = "Nombre de usuario")]
    public string Username { get; set; }

    [Required]
    [Display(Name = "Contraseña")]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [Display(Name = "Recordar")]
    public bool RememberMe { get; set; }

    [Display(AutoGenerateField = false)]
    public string ReturnUrl { get; set; }
}
