using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MyDomainPasswordChange.Models;
public record CreateAccountModel
{
    /// <summary>
    /// The account name (sAMAccountName) of the user.
    /// </summary>
    [Required]
    [Display(Name = "Nombre de cuenta.", Prompt = "Ej. jperez")]
    public required string AccountName { get; set; }

    /// <summary>
    /// The display name of the user.
    /// </summary>
    public string DisplayName => $"{FirstName} {LastName}";

    /// <summary>
    /// The user first name and middle name if case.
    /// </summary>
    [Required]
    [Display(Name = "Nombre", Prompt = "Ej. Juan")]
    public required string FirstName { get; set; }

    /// <summary>
    /// The user last names.
    /// </summary>
    [Required]
    [Display(Name = "Apellidos", Prompt = "Ej. Perez Rodriguez")]
    public required string LastName { get; set; }

    /// <summary>
    /// The description of the user.
    /// </summary>
    [Required]
    [Display(Name = "Descripción", Prompt = "Ej. Obrero C en cosas sin importancia del area productiva")]
    public required string Description { get; set; }

    /// <summary>
    /// The user's email address.
    /// </summary>
    [Required]
    [Display(Name = "Correo electrónico", Prompt = "Ej. jperez@ingeco.cu")]
    [ReadOnly(true)]
    public required string Email { get; set; }

    /// <summary>
    /// The user personal id.
    /// </summary>
    [Required]
    [Display(Name = "Carné de identidad.", Prompt = "Ej. 84102578635")]
    public required string PersonalId { get; set; }

    /// <summary>
    /// The user current job title.
    /// </summary>
    [Required]
    [Display(Name = "Cargo", Prompt = "Ej. Obrero C en cosas sin importancia del area productiva")]
    public required string JobTitle { get; set; }

    /// <summary>
    /// The name of the office where the user works.
    /// </summary>
    [Required]
    [Display(Name = "Oficina", Prompt = "Ej. Producción")]
    public required string Office { get; set; }

    /// <summary>
    /// The home address of the user.
    /// </summary>
    [Required]
    [Display(Name = "Dirección particular", Prompt = "Ej. Calle La Deseperanza No. 35 e/El Desespero y La Tristeza Rpto. Cualquiera")]
    public required string Address { get; set; }

    /// <summary>
    /// Defines the workstations that the user is allowed to login to.
    /// </summary>
    [Display(Name = "PC en las que inicia sesión")]
    public List<string> AllowedWorkstations { get; set; } = ["NONE"];

    /// <summary>
    /// The password that will be used for the user.
    /// </summary>
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public required string Password { get; set; }

    /// <summary>
    /// The password that will be used for the user.
    /// </summary>
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar Contraseña")]
    [Compare("Password", ErrorMessage = "Debe de confirmar la nueva contraseña correctamente.")]
    public string ConfirmPassword { get; set; }

    /// <summary>
    /// The identifier of the dependency.
    /// </summary>
    [Display(Name = "Dependencia")]
    public required string DependencyId { get; set; }

    /// <summary>
    /// The identifier of the dependency's area.
    /// </summary>
    [Display(Name = "Área")]
    public required string AreaId { get; set; }

    /// <summary>
    /// The groups where the user 
    /// </summary>
    public string[] GroupsId { get; set; } = [];

    /// <summary>
    /// Level of internet access.
    /// </summary>
    [Display(Name = "Acceso a internet")]
    public InternetAccess InternetAccess { get; set; }

    [Display(Name = "Acceso a la nube empresarial", Prompt = "Nube empresarial")]
    public bool CloudAccess { get; set; } = true;

    [Display(Name = "Acceso al servidor FTP (publica)", Prompt = "Acceso FTP")]
    public bool FTPAccess { get; set; } = true;

    [Display(Name = "Acceso al servidor de mensajería (jabber)", Prompt = "Acceso Mensajería Jabber")]
    public bool JabberAccess { get; set; } = true;

    [Display(Name = "Acceso al servidor multimedia", Prompt = "Acceso Multimedia")]
    public bool MediaAccess { get; set; } = true;
}
