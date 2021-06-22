using Microsoft.AspNetCore.Cors.Infrastructure;
using Stl.Fusion.Bridge;

namespace Stl.Fusion.Server
{
    public static class CorsPolicyBuilderEx
    {
        public static CorsPolicyBuilder WithFusionHeaders(this CorsPolicyBuilder builder)
            => builder.WithExposedHeaders(
                FusionHeaders.RequestPublication,
                FusionHeaders.Publication
            );
    }
}
