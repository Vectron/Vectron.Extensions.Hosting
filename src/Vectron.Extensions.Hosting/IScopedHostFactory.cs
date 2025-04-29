using Microsoft.Extensions.DependencyInjection;

namespace Vectron.Extensions.Hosting;

/// <summary>
/// A function that will be run after the <see cref="IServiceScope"/> is created, but before the <see cref="IScopedHost"/> is started.
/// </summary>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
/// <param name="cancellationToken">A <see cref="CancellationToken"/> for stopping the setup.</param>
/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
public delegate Task SetupScopeFunc(IServiceProvider serviceProvider, CancellationToken cancellationToken);

/// <summary>
/// A factory for creating and starting a scoped host.
/// </summary>
public interface IScopedHostFactory
{
    /// <summary>
    /// Create a new <see cref="IServiceScope"/> and run the <see cref="IScopedHost"/> inside there.
    /// </summary>
    /// <param name="setupAction">A function that will be run after the <see cref="IServiceScope"/> is created, but before the <see cref="IScopedHost"/> is started.</param>
    /// <param name="cancellationToken">The token to trigger shutdown.</param>
    /// <returns>A <see cref="Task"/> that will be completed when the <see cref="IScopedHost"/> stops.</returns>
    public Task RunScopedHostAsync(SetupScopeFunc setupAction, CancellationToken cancellationToken);
}
