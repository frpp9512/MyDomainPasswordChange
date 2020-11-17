using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange
{
    public class IpAddressBlacklist : IIpAddressBlacklist
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private string FilePath => Path.Combine(_webHostEnvironment.ContentRootPath, "blacklist.json");

        private List<BlacklistedIpAddress> BlacklistedIps { get; set; } = new List<BlacklistedIpAddress>();

        public IpAddressBlacklist(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
            LoadBlacklistFromFile();
        }

        private void LoadBlacklistFromFile()
        {
            if (File.Exists(FilePath))
            {
                var content = File.ReadAllText(FilePath);
                BlacklistedIps = JsonConvert.DeserializeObject<List<BlacklistedIpAddress>>(content);
            }
        }

        private void SaveBlacklistToFile()
        {
            var content = JsonConvert.SerializeObject(BlacklistedIps);
            File.WriteAllText(FilePath, content);
        }

        public void AddIpAddressToBlacklist(string ipAddress, string reason)
        {
            if (BlacklistedIps is null)
            {
                BlacklistedIps = new List<BlacklistedIpAddress>();
            }
            if (IsIpAddressBlacklisted(ipAddress))
            {
                var blacklisted = BlacklistedIps.Find(b => b.IpAddress == ipAddress);
                blacklisted.AddedInBlacklist = DateTime.Now;
                blacklisted.Reason = reason;
            }
            else
            {
                BlacklistedIps.Add(new BlacklistedIpAddress 
                {
                    AddedInBlacklist = DateTime.Now,
                    IpAddress = ipAddress,
                    Reason = reason
                });
            }
            SaveBlacklistToFile();
        }

        
        public List<BlacklistedIpAddress> GetBlacklistedIpAddresses() => BlacklistedIps;

        public bool IsIpAddressBlacklisted(string ipAddress) => BlacklistedIps?.Any(b => b.IpAddress == ipAddress) == true;

        public void RemoveIpAddressFromBlacklist(string ipAddress)
        {
            if (IsIpAddressBlacklisted(ipAddress))
            {
                BlacklistedIps.RemoveAll(b => b.IpAddress == ipAddress);
            }
            SaveBlacklistToFile();
        }
    }
}