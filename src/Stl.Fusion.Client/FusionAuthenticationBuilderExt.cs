using Stl.Fusion.Authentication;
using Stl.Fusion.Client.Internal;

namespace Stl.Fusion.Client
{
    public static class FusionAuthenticationBuilderExt
    {
        public static FusionAuthenticationBuilder AddRestEaseClient(this FusionAuthenticationBuilder fusionAuth)
        {
            var fusionClient = fusionAuth.Fusion.AddRestEaseClient();
            fusionClient.AddReplicaService<IAuthService, IAuthClientDef>();
            return fusionAuth;
        }
    }
}
