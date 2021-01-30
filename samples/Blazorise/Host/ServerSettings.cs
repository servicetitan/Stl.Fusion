using System;
using System.Text;
using Stl.DependencyInjection;

namespace Templates.Blazor2.Host
{
    [Settings("Server")]
    public class ServerSettings
    {
        public string PublisherId { get; set; } = "p";

        public string GoogleClientId { get; set; } = "77906554119-0jeq7cafi2l3qdtotmc8ndnpnvtkcvg8.apps.googleusercontent.com";
        public string GoogleClientSecret { get; set; } = "QJN0xpwyTtLTuZGV_pkWq3nF";

        public string MicrosoftAccountClientId { get; set; } = "c17d7c8e-de2c-42e3-9859-9437f03fb9a8";
        public string MicrosoftAccountClientSecret { get; set; } = "sjOGUrvbdh9U.~U6OXr~5hQ7v-DxzE7.tC";

        public string GitHubClientId { get; set; } = "7a38bc415f7e1200fee2";
        public string GitHubClientSecret { get; set; } =
            Encoding.UTF8.GetString(Convert.FromBase64String(
                "OGNkMTAzM2JmZjljOTk3ODc5MjhjNTNmMmE3Y2Q1NWU0ZmNlNjU0OA=="));
    }
}
