using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyDomainPasswordChange.Data.Interfaces;
using MyDomainPasswordChange.Extensions;
using MyDomainPasswordChange.Models;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Controllers;

[Authorize(Roles = "GlobalAdmin")]
public class BlacklistController(IIpAddressBlacklist blacklist) : Controller
{
    private readonly IIpAddressBlacklist _blacklist = blacklist;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
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
    public IActionResult Blacklisted(string address) => View(model: address);
}