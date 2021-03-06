﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MyDomainPasswordChange
{
    public class BlacklistFilter : ActionFilterAttribute
    {
        private readonly IIpAddressBlacklist _blacklist;

        public BlacklistFilter(IIpAddressBlacklist blacklist)
        {
            _blacklist = blacklist;
        }

        public override void OnResultExecuting(ResultExecutingContext context)
        {
            var ipAddress = context.HttpContext.Connection.RemoteIpAddress.ToString();
            if (_blacklist.IsIpAddressBlacklisted(ipAddress))
            {
                context.Result = new StatusCodeResult((int)StatusCodes.Status403Forbidden);
            }
            base.OnResultExecuting(context);
        }
    }
}
