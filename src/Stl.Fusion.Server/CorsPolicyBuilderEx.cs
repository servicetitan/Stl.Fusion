using Microsoft.AspNetCore.Cors.Infrastructure;
using Stl.Fusion.Client;

namespace Stl.Fusion.Server
{
    public static class CorsPolicyBuilderEx
    {
        public static CorsPolicyBuilder WithFusionHeaders(this CorsPolicyBuilder builder)
            => builder.WithExposedHeaders(
                HttpHeaderNames.PublisherId,
                HttpHeaderNames.PublicationId,
                HttpHeaderNames.LTag,
                HttpHeaderNames.IsConsistent
            );
    }
}
