﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MyDomainPasswordChange.Data.Interfaces;

namespace MyDomainPasswordChange.Filters;

public class BlacklistFilter : ActionFilterAttribute
{
    private readonly IIpAddressBlacklist _blacklist;

    public BlacklistFilter(IIpAddressBlacklist blacklist) => _blacklist = blacklist;

    public override async void OnResultExecuting(ResultExecutingContext context)
    {
        var ipAddress = context.HttpContext.Connection.RemoteIpAddress.ToString();
        if (await _blacklist.IsBlacklistedAsync(ipAddress))
        {
            context.Result = new RedirectToActionResult("Blacklisted", "Blacklist", new { address = ipAddress });
        }
        //base.OnResultExecuting(context);
    }
}