namespace Vectron.Extensions.Hosting;

/// <summary>
/// A program abstraction.
/// </summary>
public interface IScopedHost : IDisposable
{
    /// <summary>
    /// Gets the services configured for the program />).
    /// </summary>
    IServiceProvider Services
    {
        get;
    }

    /// <summary>
    /// Starts the <see cref="IScopedHostedService"/> objects configured for the program. The application
    /// will run until interrupted or until <see cref="IScopedHostScopeLifetime.StopScope()"/>
    /// is called.
    /// </summary>
    /// <param name="cancellationToken">Used to abort program start.</param>
    /// <returns>A <see cref="Task"/> that will be completed when the <see cref="IScopedHost"/> starts.</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to gracefully stop the program.
    /// </summary>
    /// <param name="cancellationToken">Used to indicate when stop should no longer be graceful.</param>
    /// <returns>A <see cref="Task"/> that will be completed when the <see cref="IScopedHost"/> stops.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);
}
