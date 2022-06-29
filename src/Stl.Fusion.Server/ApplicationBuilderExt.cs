using Microsoft.AspNetCore.Builder;
using Stl.Fusion.Server.Authentication;

namespace Stl.Fusion.Server;

public static class ApplicationBuilderExt
{
    public static IApplicationBuilder UseFusionSession(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SessionMiddleware>();
    }
}
