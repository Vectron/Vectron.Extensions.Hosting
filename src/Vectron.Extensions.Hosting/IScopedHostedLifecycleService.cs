using Microsoft.Extensions.Hosting;

namespace Vectron.Extensions.Hosting;

/// <summary>
/// Defines methods that are run before or after
/// <see cref="IScopedHostedService.StartAsync(CancellationToken)"/> and <see cref="IScopedHostedService.StopAsync(CancellationToken)"/>.
/// </summary>
public interface IScopedHostedLifecycleService : IHostedService
{
    /// <summary>
    /// Triggered after <see cref="IHostedService.StartAsync(CancellationToken)"/>.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    Task StartedAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Triggered before <see cref="IHostedService.StartAsync(CancellationToken)"/>.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    Task StartingAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Triggered after <see cref="IHostedService.StopAsync(CancellationToken)"/>.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the stop process has been aborted.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    Task StoppedAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Triggered before <see cref="IHostedService.StopAsync(CancellationToken)"/>.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    Task StoppingAsync(CancellationToken cancellationToken);
}
