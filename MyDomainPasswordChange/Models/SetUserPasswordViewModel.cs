using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Models
{
    public class SetUserPasswordViewModel : UserViewModel
    {
        [Required]
        [Display(Name = "Contraseña")]
        [DataType(DataType.Password)]
        [MinLength(8, ErrorMessage = "La contraseña debe de contener al menos 8 caracteres.")]
        public string Password { get; set; }

        [Required]
        [Display(Name = "Confirmar contraseña")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Debe de confirmar la contraseña correctamente.")]
        public string ConfirmPassword { get; set; }
    }
}
