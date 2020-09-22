using Stl.Fusion.Authentication;
using Stl.Fusion.Client.Authentication;

namespace Stl.Fusion.Client
{
    public static class FusionAuthenticationBuilderEx
    {
        public static FusionAuthenticationBuilder AddClient(this FusionAuthenticationBuilder fusionAuth)
        {
            var fusionClient = fusionAuth.Fusion.AddRestEaseClient();
            fusionClient.AddReplicaService<IAuthService, IAuthClient>();
            return fusionAuth;
        }
    }
}
