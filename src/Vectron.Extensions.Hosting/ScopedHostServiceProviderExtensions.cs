using Microsoft.Extensions.DependencyInjection;

namespace Vectron.Extensions.Hosting;

/// <summary>
/// Extension methods for <see cref="IServiceProvider"/> for running the scoped host.
/// </summary>
public static class ScopedHostServiceProviderExtensions
{
    /// <summary>
    /// Create a new <see cref="IServiceScope"/> and run the <see cref="IScopedHost"/> inside there.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
    /// <param name="token">The token to trigger shutdown.</param>
    /// <returns>A <see cref="Task"/> that will be completed when the <see cref="IScopedHost"/> stops.</returns>
    [Obsolete("RunScopedHost is deprecated, inject 'IScopedHostFactory'.")]
    public static Task RunScopedHost(this IServiceProvider serviceProvider, CancellationToken token)
        => serviceProvider.RunScopedHost(s => Task.CompletedTask, token);

    /// <summary>
    /// Create a new <see cref="IServiceScope"/> and run the <see cref="IScopedHost"/> inside there.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
    /// <param name="setupScope">A function that will be run after the <see cref="IServiceScope"/> is created, but before the <see cref="IScopedHost"/> is started.</param>
    /// <param name="token">The token to trigger shutdown.</param>
    /// <returns>A <see cref="Task"/> that will be completed when the <see cref="IScopedHost"/> stops.</returns>
    [Obsolete("RunScopedHost is deprecated, inject 'IScopedHostFactory'.")]
    public static Task RunScopedHost(this IServiceProvider serviceProvider, Func<IServiceProvider, Task> setupScope, CancellationToken token)
        => serviceProvider.RunScopedHost((s, t) => setupScope(s), token);

    /// <summary>
    /// Create a new <see cref="IServiceScope"/> and run the <see cref="IScopedHost"/> inside there.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
    /// <param name="setupScope">A function that will be run after the <see cref="IServiceScope"/> is created, but before the <see cref="IScopedHost"/> is started.</param>
    /// <param name="token">The token to trigger shutdown.</param>
    /// <returns>A <see cref="Task"/> that will be completed when the <see cref="IScopedHost"/> stops.</returns>
    [Obsolete("RunScopedHost is deprecated, inject 'IScopedHostFactory'.")]
    public static Task RunScopedHost(this IServiceProvider serviceProvider, SetupScopeFunc setupScope, CancellationToken token)
    {
        var factory = serviceProvider.GetRequiredService<IScopedHostFactory>();
        return factory.RunScopedHostAsync(setupScope, token);
    }
}
