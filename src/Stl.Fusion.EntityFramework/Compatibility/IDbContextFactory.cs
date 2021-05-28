#if NETSTANDARD2_0

namespace Microsoft.EntityFrameworkCore
{
    /// <summary>
    ///     Defines a factory for creating <see cref="T:Microsoft.EntityFrameworkCore.DbContext" /> instances.
    ///     A service of this type is registered in the dependency injection container by the
    ///     <see cref="M:EntityFrameworkServiceCollectionExtensions.AddDbContextPool" /> methods.
    /// </summary>
    /// <typeparam name="TContext"> The <see cref="T:Microsoft.EntityFrameworkCore.DbContext" /> type to create. </typeparam>
    public interface IDbContextFactory<out TContext> where TContext : DbContext
    {
        /// <summary>
        ///     <para>
        ///         Creates a new <see cref="T:Microsoft.EntityFrameworkCore.DbContext" /> instance.
        ///     </para>
        ///     <para>
        ///         The caller is responsible for disposing the context; it will not be disposed by the dependency injection container.
        ///     </para>
        /// </summary>
        /// <returns> A new context instance. </returns>
        TContext CreateDbContext();
    }
}

#endif