using System;
using System.Text;
using Stl.DependencyInjection;

namespace Templates.Blazor2.Host
{
    [Settings("Server")]
    public class ServerSettings
    {
        public bool UseInMemoryAuthService { get; set; } = false;
        public string PublisherId { get; set; } = "p";

        public string GoogleClientId { get; set; } = "77906554119-0jeq7cafi2l3qdtotmc8ndnpnvtkcvg8.apps.googleusercontent.com";
        public string GoogleClientSecret { get; set; } =
            Encoding.UTF8.GetString(Convert.FromBase64String(
                "UUpOMHhwd3lUdExUdVpHVl9wa1dxM25G"));

        public string MicrosoftAccountClientId { get; set; } = "6839dbf7-d1d3-4eb2-a7e1-ce8d48f34d00";
        public string MicrosoftAccountClientSecret { get; set; } =
            Encoding.UTF8.GetString(Convert.FromBase64String(
                "REFYeH4yNTNfcVNWX2h0WkVoc1V6NHIueDN+LWRxUTA2Zw=="));

        public string GitHubClientId { get; set; } = "7a38bc415f7e1200fee2";
        public string GitHubClientSecret { get; set; } =
            Encoding.UTF8.GetString(Convert.FromBase64String(
                "OGNkMTAzM2JmZjljOTk3ODc5MjhjNTNmMmE3Y2Q1NWU0ZmNlNjU0OA=="));
    }
}
