using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyDomainPasswordChange.Models;

public class UserViewModel
{
    [Display(Name = "Nombre de usuario")]
    public string AccountName { get; set; }

    [Display(Name = "Nombre completo")]
    public string DisplayName { get; set; }

    [Display(Name = "Nombres")]
    public string FirstName { get; set; }

    [Display(Name = "Apellidos")]
    public string LastName { get; set; }

    [Display(Name = "Descripción")]
    public string Description { get; set; }

    [Display(Name = "Correo electrónico")]
    public string Email { get; set; }

    [Display(Name = "Carné de identidad")]
    public string PersonalId { get; set; }

    [Display(Name = "Cargo")]
    public string JobTitle { get; set; }

    [Display(Name = "Oficina")]
    public string Office { get; set; }

    [Display(Name = "Dirección particular")]
    public string Address { get; set; }

    [Display(Name = "Fecha de cambio de contraseña")]
    public DateTime LastPasswordSet { get; set; }

    [Display(Name = "La contraseña nunca expira")]
    public bool PasswordNeverExpires { get; set; }

    [Display(Name = "PC en las que inicia sesión")]
    public List<string> AllowedWorkstations { get; set; } = new List<string> { "NONE" };

    [Display(Name = "Capacidad del buzón de correo")]
    public string MailboxCapacity { get; set; }

    [Display(Name = "Habilitado")]
    public bool Enabled { get; init; }

    [Display(Name = "Días ha expirar la contraseña")]
    public int PasswordExpirationDays { get; set; }

    [Display(Name = "Contraseña pendiente a establecer")]
    public bool PendingToSetPassword => new DateTime(1, 1, 1, 0, 0, 0) == LastPasswordSet;

    [Display(Name = "Acceso a internet")]
    public InternetAccess InternetAccess { get; set; } = InternetAccess.None;

    [Display(Name = "Grupos")]
    public List<GroupModel> Groups { get; set; }
}

public enum InternetAccess
{
    None,
    National,
    Restricted,
    Full
}