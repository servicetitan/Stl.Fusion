using System;
using Stl.Fusion.Authentication;

namespace Stl.Fusion
{
    public static class FusionBuilderEx
    {
        public static FusionAuthenticationBuilder AddAuthentication(this FusionBuilder fusion)
            => new(fusion);

        public static FusionBuilder AddAuthentication(this FusionBuilder fusion,
            Action<FusionAuthenticationBuilder> configureFusionAuthentication)
        {
            var fusionAuth = fusion.AddAuthentication();
            configureFusionAuthentication.Invoke(fusionAuth);
            return fusion;
        }
    }
}
