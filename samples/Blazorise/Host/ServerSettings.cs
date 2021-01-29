using Stl.DependencyInjection;

namespace Templates.Blazor2.Host
{
    [Settings("Server")]
    public class ServerSettings
    {
        public string PublisherId { get; set; } = "p";
        public string GoogleClientId { get; set; } =
            "77906554119-0jeq7cafi2l3qdtotmc8ndnpnvtkcvg8.apps.googleusercontent.com";
        public string GoogleClientSecret { get; set; } =
            "QJN0xpwyTtLTuZGV_pkWq3nF";
    }
}
