namespace Vectron.Extensions.Hosting;

/// <summary>
/// Defines methods that are run before or after <see
/// cref="IScopedHostedService.StartAsync(CancellationToken)"/> and <see cref="IScopedHostedService.StopAsync(CancellationToken)"/>.
/// </summary>
public interface IScopedHostedLifecycleService : IScopedHostedService
{
    /// <summary>
    /// Triggered after <see cref="IScopedHostedService.StartAsync(CancellationToken)"/>.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    public Task StartedAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Triggered before <see cref="IScopedHostedService.StartAsync(CancellationToken)"/>.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    public Task StartingAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Triggered after <see cref="IScopedHostedService.StopAsync(CancellationToken)"/>.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the stop process has been aborted.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    public Task StoppedAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Triggered before <see cref="IScopedHostedService.StopAsync(CancellationToken)"/>.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    public Task StoppingAsync(CancellationToken cancellationToken);
}
