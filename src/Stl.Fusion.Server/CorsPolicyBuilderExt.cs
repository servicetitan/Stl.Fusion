using Microsoft.AspNetCore.Cors.Infrastructure;
using Stl.Fusion.Bridge;

namespace Stl.Fusion.Server;

public static class CorsPolicyBuilderExt
{
    public static CorsPolicyBuilder WithFusionHeaders(this CorsPolicyBuilder builder)
        => builder
            .WithHeaders(FusionHeaders.RequestPublication)
            .WithExposedHeaders(FusionHeaders.Publication);
}
