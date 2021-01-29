using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.Fusion.Server.Authentication
{
    public static class HttpContextEx
    {
        public static async Task<AuthenticationScheme[]> GetAuthenticationSchemesAsync(this HttpContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var schemes = context.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
            return (
                from scheme in await schemes.GetAllSchemesAsync()
                where !string.IsNullOrEmpty(scheme.DisplayName)
                select scheme
                ).ToArray();
        }

        public static async Task<bool> IsAuthenticationSchemeSupportedAsync(this HttpContext context, string scheme)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            return (
                from s in await context.GetAuthenticationSchemesAsync()
                where string.Equals(s.Name, scheme, StringComparison.OrdinalIgnoreCase)
                select s
                ).Any();
        }
    }
}
