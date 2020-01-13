﻿using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using WB.Core.Infrastructure.Modularity;

namespace WB.UI.Shared.Web.UnderConstruction
{
    public class UnderConstructionMiddleware
    {
        private readonly RequestDelegate next;
        private readonly UnderConstructionInfo underConstructionInfo;

        public UnderConstructionMiddleware(RequestDelegate next, UnderConstructionInfo underConstructionInfo)
        {
            this.next = next;
            this.underConstructionInfo = underConstructionInfo;
        }

        public async Task Invoke(HttpContext context)
        {
            if (underConstructionInfo.Status != UnderConstructionStatus.Finished)
            {
                context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                context.Response.Headers.Add("Retry-After", "30");
                context.Request.Path = "/UnderConstruction";
            }

            await next.Invoke(context);
        }
    }
}
