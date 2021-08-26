using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Data.Models
{
    public class BlacklistedIpAddress
    {
        [Key]
        public Guid Id { get; set; }
        public DateTimeOffset AddedInBlacklist { get; set; } = DateTime.Now;
        public string IpAddress { get; set; }
        public string Reason { get; set; }
    }
}
