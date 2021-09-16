using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.Fusion.Server.Authentication
{
    public static class HttpContextExt
    {
        public static async Task<AuthenticationScheme[]> GetAuthenticationSchemas(this HttpContext httpContext)
        {
            if (httpContext == null)
                throw new ArgumentNullException(nameof(httpContext));
            var schemes = httpContext.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
            return (
                from scheme in await schemes.GetAllSchemesAsync()
                where !string.IsNullOrEmpty(scheme.DisplayName)
                select scheme
                ).ToArray();
        }

        public static async Task<bool> IsAuthenticationSchemeSupported(this HttpContext httpContext, string scheme)
        {
            if (httpContext == null)
                throw new ArgumentNullException(nameof(httpContext));
            return (
                from s in await httpContext.GetAuthenticationSchemas()
                where string.Equals(s.Name, scheme, StringComparison.OrdinalIgnoreCase)
                select s
                ).Any();
        }
    }
}
