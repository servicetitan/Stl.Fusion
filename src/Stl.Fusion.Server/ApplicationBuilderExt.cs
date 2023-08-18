using Microsoft.AspNetCore.Builder;
using Stl.Fusion.Server.Middlewares;

namespace Stl.Fusion.Server;

public static class ApplicationBuilderExt
{
    public static IApplicationBuilder UseFusionSession(this IApplicationBuilder app)
        => app.UseMiddleware<SessionMiddleware>();
}
