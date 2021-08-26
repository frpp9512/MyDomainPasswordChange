using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyDomainPasswordChange.Data.Interfaces;
using MyDomainPasswordChange.Data.Models;
using MyDomainPasswordChange.Extensions;
using MyDomainPasswordChange.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Controllers
{
    [Authorize(Roles = "GlobalAdmin")]
    public class BlacklistController : Controller
    {
        private readonly IIpAddressBlacklist _blacklist;

        public BlacklistController(IIpAddressBlacklist blacklist)
        {
            _blacklist = blacklist;
        }

        private void GenerateDummyData(int amount)
        {
            var generateRandomAddress = new Func<string>(() =>
            {
                var random = new Random();
                return $"{random.Next(10, 254)}.{random.Next(0, 255)}.{random.Next(0, 255)}.{random.Next(0, 255)}";
            });
            var reasons = new string[] 
            { 
                "All the reasons are good",
                "Anything but this", 
                "Because I want it", 
                "I really can't make any other", 
                "For being bad", 
                "Bad luck this time" 
            };
            var blacklistAddresses = new List<BlacklistedIpAddress>();
            var random = new Random();
            for (int i = 0; i < amount; i++)
            {
                blacklistAddresses.Add(new() 
                {
                    Id = Guid.NewGuid(),
                    AddedInBlacklist = DateTime.Now,
                    IpAddress = generateRandomAddress(),
                    Reason = reasons[random.Next(0, reasons.Length - 1)]
                });
            }
            blacklistAddresses.ForEach(b => _blacklist.AddIpAddressAsync(b));
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            //GenerateDummyData(7);

            var blacklist = await _blacklist.GetIpAddressesAsync();
            var vm = new BlacklistIndexViewModel 
            {
                BlacklistedIpAddresses = blacklist.Select(b => b.GetViewModel())
            };
            return View(vm);
        }

        [HttpGet]
        public IActionResult Create()
            => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BlacklistedIpViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                if (IPAddress.TryParse(viewModel.IpAddress, out _))
                {
                    var model = viewModel.GetModel();
                    model.AddedInBlacklist = DateTime.Now;
                    await _blacklist.AddIpAddressAsync(model);
                    TempData["BlacklistCreated"] = model.IpAddress;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("BadFormat", "La dirección IP debe de estar en el formato correcto.");
                }
            }
            return View(viewModel);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (await _blacklist.ExistsBlacklistedAddressAsync(id))
            {
                var address = await _blacklist.GetBlacklistedIpAddressAsync(id);
                await _blacklist.RemoveBlacklistedAddressAsync(address);
                return Ok($"Se ha eliminado la dirección {address.IpAddress} de la lista negra satisfactoriamente.");
            }
            return NotFound("La dirección no se encuentra en la lista negra.");
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Blacklisted(string address)
        {
            return View(model: address);
        }
    }
}