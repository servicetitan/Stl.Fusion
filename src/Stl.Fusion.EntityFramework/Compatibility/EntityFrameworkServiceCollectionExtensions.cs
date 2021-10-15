#if NETSTANDARD2_0

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     Extension methods for setting up Entity Framework related services in an <see cref="IServiceCollection" />.
/// </summary>
public static class EntityFrameworkServiceCollectionExtensions
{
    private static void AddPoolingOptions<TContext>(
        IServiceCollection serviceCollection,
        Action<IServiceProvider, DbContextOptionsBuilder> optionsAction,
        int poolSize)
        where TContext : DbContext
    {
        if (poolSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(poolSize), CoreStrings.InvalidPoolSize);

        CheckContextConstructors<TContext>();

        AddCoreServices<TContext>(
            serviceCollection,
            (sp, ob) =>
            {
                optionsAction(sp, ob);

                var extension = (ob.Options.FindExtension<CoreOptionsExtension>() ?? new CoreOptionsExtension())
                    .WithMaxPoolSize(poolSize);

                ((IDbContextOptionsBuilderInfrastructure)ob).AddOrUpdateExtension(extension);
            },
            ServiceLifetime.Singleton);
    }

    /// <summary>
    ///     <para>
    ///         Registers an <see cref="IDbContextFactory{TContext}" /> in the <see cref="IServiceCollection" /> to create instances
    ///         of given <see cref="DbContext" /> type where instances are pooled for reuse.
    ///     </para>
    ///     <para>
    ///         Registering a factory instead of registering the context type directly allows for easy creation of new
    ///         <see cref="DbContext" /> instances.
    ///         Registering a factory is recommended for Blazor applications and other situations where the dependency
    ///         injection scope is not aligned with the context lifetime.
    ///     </para>
    ///     <para>
    ///         Use this method when using dependency injection in your application, such as with Blazor.
    ///         For applications that don't use dependency injection, consider creating <see cref="DbContext" />
    ///         instances directly with its constructor. The <see cref="DbContext.OnConfiguring" /> method can then be
    ///         overridden to configure a connection string and other options.
    ///     </para>
    ///     <para>
    ///         For more information on how to use this method, see the Entity Framework Core documentation at https://aka.ms/efdocs.
    ///         For more information on using dependency injection, see https://go.microsoft.com/fwlink/?LinkId=526890.
    ///     </para>
    /// </summary>
    /// <typeparam name="TContext"> The type of <see cref="DbContext" /> to be created by the factory. </typeparam>
    /// <param name="serviceCollection"> The <see cref="IServiceCollection" /> to add services to. </param>
    /// <param name="optionsAction">
    ///     <para>
    ///         A required action to configure the <see cref="DbContextOptions" /> for the context. When using
    ///         context pooling, options configuration must be performed externally; <see cref="DbContext.OnConfiguring" />
    ///         will not be called.
    ///     </para>
    /// </param>
    /// <param name="poolSize">
    ///     Sets the maximum number of instances retained by the pool.
    /// </param>
    /// <returns>
    ///     The same service collection so that multiple calls can be chained.
    /// </returns>
    public static IServiceCollection AddPooledDbContextFactory<TContext>(
        this IServiceCollection serviceCollection,
        Action<DbContextOptionsBuilder> optionsAction,
        int poolSize = 128)
        where TContext : DbContext
    {
        if (optionsAction == null) throw new ArgumentNullException(nameof(optionsAction));

        return AddPooledDbContextFactory<TContext>(serviceCollection, (_, ob) => optionsAction(ob), poolSize);
    }

    /// <summary>
    ///     <para>
    ///         Registers an <see cref="IDbContextFactory{TContext}" /> in the <see cref="IServiceCollection" /> to create instances
    ///         of given <see cref="DbContext" /> type where instances are pooled for reuse.
    ///     </para>
    ///     <para>
    ///         Registering a factory instead of registering the context type directly allows for easy creation of new
    ///         <see cref="DbContext" /> instances.
    ///         Registering a factory is recommended for Blazor applications and other situations where the dependency
    ///         injection scope is not aligned with the context lifetime.
    ///     </para>
    ///     <para>
    ///         Use this method when using dependency injection in your application, such as with Blazor.
    ///         For applications that don't use dependency injection, consider creating <see cref="DbContext" />
    ///         instances directly with its constructor. The <see cref="DbContext.OnConfiguring" /> method can then be
    ///         overridden to configure a connection string and other options.
    ///     </para>
    ///     <para>
    ///         For more information on how to use this method, see the Entity Framework Core documentation at https://aka.ms/efdocs.
    ///         For more information on using dependency injection, see https://go.microsoft.com/fwlink/?LinkId=526890.
    ///     </para>
    /// </summary>
    /// <typeparam name="TContext"> The type of <see cref="DbContext" /> to be created by the factory. </typeparam>
    /// <param name="serviceCollection"> The <see cref="IServiceCollection" /> to add services to. </param>
    /// <param name="optionsAction">
    ///     <para>
    ///         A required action to configure the <see cref="DbContextOptions" /> for the context. When using
    ///         context pooling, options configuration must be performed externally; <see cref="DbContext.OnConfiguring" />
    ///         will not be called.
    ///     </para>
    /// </param>
    /// <param name="poolSize">
    ///     Sets the maximum number of instances retained by the pool.
    /// </param>
    /// <returns>
    ///     The same service collection so that multiple calls can be chained.
    /// </returns>
    public static IServiceCollection AddPooledDbContextFactory<TContext>(
        this IServiceCollection serviceCollection,
        Action<IServiceProvider, DbContextOptionsBuilder> optionsAction,
        int poolSize = 128)
        where TContext : DbContext
    {
        if (serviceCollection == null) throw new ArgumentNullException(nameof(serviceCollection));
        if (optionsAction == null) throw new ArgumentNullException(nameof(optionsAction));

        AddPoolingOptions<TContext>(serviceCollection, optionsAction, poolSize);

        serviceCollection.TryAddSingleton<DbContextPool<TContext>>();
        serviceCollection.TryAddSingleton<IDbContextFactory<TContext>, PooledDbContextFactory<TContext>>();

        return serviceCollection;
    }

    private static void AddCoreServices<TContextImplementation>(
        IServiceCollection serviceCollection,
        Action<IServiceProvider, DbContextOptionsBuilder>? optionsAction,
        ServiceLifetime optionsLifetime)
        where TContextImplementation : DbContext
    {
        serviceCollection.TryAdd(
            new ServiceDescriptor(
                typeof(DbContextOptions<TContextImplementation>),
                p => CreateDbContextOptions<TContextImplementation>(p, optionsAction),
                optionsLifetime));

        serviceCollection.Add(
            new ServiceDescriptor(
                typeof(DbContextOptions),
                p => p.GetRequiredService<DbContextOptions<TContextImplementation>>(),
                optionsLifetime));
    }

    private static DbContextOptions<TContext> CreateDbContextOptions<TContext>(
        IServiceProvider applicationServiceProvider,
        Action<IServiceProvider, DbContextOptionsBuilder>? optionsAction)
        where TContext : DbContext
    {
        var builder = new DbContextOptionsBuilder<TContext>(
            new DbContextOptions<TContext>(new Dictionary<Type, IDbContextOptionsExtension>()));

        builder.UseApplicationServiceProvider(applicationServiceProvider);

        optionsAction?.Invoke(applicationServiceProvider, builder);

        return builder.Options;
    }

    private static void CheckContextConstructors<TContext>()
        where TContext : DbContext
    {
        var declaredConstructors = typeof(TContext).GetTypeInfo().DeclaredConstructors.ToList();
        if (declaredConstructors.Count == 1
            && declaredConstructors[0].GetParameters().Length == 0)
        {
            throw new ArgumentException(CoreStrings.DbContextMissingConstructor(typeof(TContext).ShortDisplayName()));
        }
    }
}

#endif
