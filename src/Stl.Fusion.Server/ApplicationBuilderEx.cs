using Microsoft.AspNetCore.Builder;
using Stl.Fusion.Server.Authentication;

namespace Stl.Fusion.Server
{
    public static class ApplicationBuilderEx
    {
        public static IApplicationBuilder UseFusionSession(this IApplicationBuilder app)
            => app.UseMiddleware<FusionSessionMiddleware>();
    }
}
