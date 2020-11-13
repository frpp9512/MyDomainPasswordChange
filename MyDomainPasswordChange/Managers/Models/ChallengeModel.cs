using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange
{
    public class ChallengeModel
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string Answer { get; set; }
    }
}
