using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Data.Models
{
    public class PasswordHistoryEntry
    {
        [Key]
        public Guid Id { get; set; }
        public DateTime Updated { get; set; }
        public string AccountName { get; set; }
        public string Password { get; set; }
    }
}
