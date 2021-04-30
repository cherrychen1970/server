﻿using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Bit.Core.Utilities
{
    public class SessionContextMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionContextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext, SessionContext currentContext, GlobalSettings globalSettings)
        {
            await currentContext.BuildAsync(httpContext, globalSettings);
            await _next.Invoke(httpContext);
        }
    }
}
