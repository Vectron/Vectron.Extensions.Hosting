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
    public static Task RunScopedHost(this IServiceProvider serviceProvider, CancellationToken token)
        => serviceProvider.RunScopedHost(s => Task.CompletedTask, token);

    /// <summary>
    /// Create a new <see cref="IServiceScope"/> and run the <see cref="IScopedHost"/> inside there.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
    /// <param name="setupScope">A function that will be run after the <see cref="IServiceScope"/> is created, but before the <see cref="IScopedHost"/> is started.</param>
    /// <param name="token">The token to trigger shutdown.</param>
    /// <returns>A <see cref="Task"/> that will be completed when the <see cref="IScopedHost"/> stops.</returns>
    public static async Task RunScopedHost(this IServiceProvider serviceProvider, Func<IServiceProvider, Task> setupScope, CancellationToken token)
    {
        var scopeLifetime = serviceProvider.GetRequiredService<IScopeLifeTime>() as ScopeLifeTime
            ?? throw new InvalidOperationException("Replacing IScopeLifeTime is not supported.");

        var scope = serviceProvider.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            await setupScope(scope.ServiceProvider).ConfigureAwait(false);
            scopeLifetime.NotifyCreated(scope);
            var host = scope.ServiceProvider.GetRequiredService<IScopedHost>();
            await host.RunAsync(token).ConfigureAwait(false);
            scopeLifetime.NotifyDestroying(scope);
        }
    }
}
