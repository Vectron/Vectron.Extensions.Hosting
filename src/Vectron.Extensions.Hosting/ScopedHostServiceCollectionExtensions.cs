using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vectron.Extensions.Hosting.Internal;

namespace Vectron.Extensions.Hosting;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> for configuring the scoped host.
/// </summary>
public static class ScopedHostServiceCollectionExtensions
{
    /// <summary>
    /// Setup the <see cref="IScopedHost"/> services.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register with.</param>
    /// <returns>The original <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddScopedHost(this IServiceCollection services)
    {
        services.TryAddScoped<IScopedHostScopeLifetime, ScopedHostScopeLifetime>();
        services.TryAddScoped<IScopedHostLifetime, NullLifetime>();
        services.TryAddScoped<IScopedHost, ScopedHost>();
        return services;
    }

    /// <summary>
    /// Add an <see cref="IScopedHostedService"/> registration for the given type.
    /// </summary>
    /// <typeparam name="THostedService">An <see cref="IScopedHostedService"/> to register.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to register with.</param>
    /// <returns>The original <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddScopedHostedService<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THostedService>(this IServiceCollection services)
        where THostedService : class, IScopedHostedService
    {
        _ = services.AddScopedHost();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IScopedHostedService, THostedService>());
        return services;
    }

    /// <summary>
    /// Add an <see cref="IScopedHostedService"/> registration for the given type.
    /// </summary>
    /// <typeparam name="THostedService">An <see cref="IScopedHostedService"/> to register.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to register with.</param>
    /// <param name="implementationFactory">A factory to create new instances of the service implementation.</param>
    /// <returns>The original <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddScopedHostedService<THostedService>(this IServiceCollection services, Func<IServiceProvider, THostedService> implementationFactory)
        where THostedService : class, IScopedHostedService
    {
        _ = services.AddScopedHost();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IScopedHostedService>(implementationFactory));
        return services;
    }
}
