namespace Stl.Fusion.Client
{
    /// <summary>
    /// The common interface for all <see cref="ClientAccessor{TClient}"/>-s.
    /// </summary>
    public interface IClientAccessor
    {
        object Client { get; }
    }

    /// <summary>
    /// Provides direct access to RestEase client for replica services.
    /// </summary>
    /// <typeparam name="TClient">The type of the client - usually,
    /// the type of the service you want to get client for.</typeparam>
    public class ClientAccessor<TClient> : IClientAccessor
        where TClient : class
    {
        object IClientAccessor.Client => Client;
        public TClient Client { get; }

        public ClientAccessor(object client)
            => Client = (TClient) client;
    }
}
