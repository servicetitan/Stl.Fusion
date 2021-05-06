#if NETSTANDARD2_0

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Testing
{
    /// <summary>Represents a server.</summary>
  public interface IServer : IDisposable
  {
    /// <summary>A collection of HTTP features of the server.</summary>
    IFeatureCollection Features { get; }

    /// <summary>Start the server with an application.</summary>
    /// <param name="application">An instance of <see cref="T:Microsoft.AspNetCore.Hosting.Server.IHttpApplication`1" />.</param>
    /// <typeparam name="TContext">The context associated with the application.</typeparam>
    /// <param name="cancellationToken">Indicates if the server startup should be aborted.</param>
    Task StartAsync<TContext>(
      IHttpApplication<TContext> application,
      CancellationToken cancellationToken);

    /// <summary>
    /// Stop processing requests and shut down the server, gracefully if possible.
    /// </summary>
    /// <param name="cancellationToken">Indicates if the graceful shutdown should be aborted.</param>
    Task StopAsync(CancellationToken cancellationToken);
  }

    public interface IFeatureCollection : IEnumerable<KeyValuePair<Type, object>>, IEnumerable
  {
    bool IsReadOnly { get; }

    int Revision { get; }

    object? this[Type key] { get; set; }

    TFeature Get<TFeature>();

    void Set<TFeature>(TFeature instance);
  }

  public interface IServerAddressesFeature
  {
    ICollection<string> Addresses { get; }

    bool PreferHostingUrls { get; set; }
  }

    /// <summary>Represents an application.</summary>
  /// <typeparam name="TContext">The context associated with the application.</typeparam>
  public interface IHttpApplication<TContext>
  {
    /// <summary>
    /// Create a TContext given a collection of HTTP features.
    /// </summary>
    /// <param name="contextFeatures">A collection of HTTP features to be used for creating the TContext.</param>
    /// <returns>The created TContext.</returns>
    TContext CreateContext(IFeatureCollection contextFeatures);

    /// <summary>Dispose a given TContext.</summary>
    /// <param name="context">The TContext to be disposed.</param>
    /// <param name="exception">The Exception thrown when processing did not complete successfully, otherwise null.</param>
    void DisposeContext(TContext context, Exception exception);

    /// <summary>Asynchronously processes an TContext.</summary>
    /// <param name="context">The TContext that the operation will process.</param>
    Task ProcessRequestAsync(TContext context);
  }
}

#endif