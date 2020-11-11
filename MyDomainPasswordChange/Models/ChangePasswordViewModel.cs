using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Models
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Debe de especificar el nombre de usuario debidamente.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Debe escribir la contraseña actual del usuario.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Debe escribir la nueva contraseña que desea establecer.")]
        [DataType(DataType.Password)]
        [MinLength(8, ErrorMessage = "La contraseña debe de contener al menos 8 caracteres.")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Debe confirmar la nueva contraseña que desea establecer.")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Debe de confirmar la nueva contraseña correctamente.")]
        public string NewPasswordConfirm { get; set; }
    }
}
