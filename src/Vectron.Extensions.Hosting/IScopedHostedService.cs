namespace Vectron.Extensions.Hosting;

/// <summary>
/// Defines methods for objects that are managed by the scoped host.
/// </summary>
public interface IScopedHostedService
{
    /// <summary>
    /// Triggered when the application host is ready to start the service.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Triggered when the application host is performing a graceful shutdown.
    /// </summary>
    /// <param name="cancellationToken">
    /// Indicates that the shutdown process should no longer be graceful.
    /// </param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task StopAsync(CancellationToken cancellationToken);
}
