using System;
using System.ComponentModel.DataAnnotations;

namespace MyDomainPasswordChange.Models;

public record BlacklistedIpViewModel
{
    public Guid Id { get; set; }

    [Display(Name = "Agregado", Description = "La fecha y hora cuando se agregó la dirección a la lista negra")]
    public DateTimeOffset AddedInBlacklist { get; set; }

    [Display(Name = "Dirección Ip", Prompt = "Ej. 192.154.236.21", Description = "La dirección que se encuentra en lista negra.")]
    public string IpAddress { get; set; }

    [Display(Name = "Motivo", Prompt = "Ej. Sospechoso de ataque", Description = "El motivo por el cual fue agregado a la lista negra.")]
    public string Reason { get; set; }
}