namespace Vectron.Extensions.Hosting;

/// <summary>
/// Tracks scoped host lifetime.
/// </summary>
public interface IScopedHostLifetime
{
    /// <summary>
    /// Called from <see cref="IScopedHost.StopAsync(CancellationToken)"/> to indicate that the host is stopping and it's time to shut down.
    /// </summary>
    /// <param name="cancellationToken">Used to indicate when stop should no longer be graceful.</param>
    /// <returns>A <see cref="Task"/>.</returns>
    public Task StopAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Called at the start of <see cref="IScopedHost.StartAsync(CancellationToken)"/> which will wait until it's complete before
    /// continuing. This can be used to delay startup until signaled by an external event.
    /// </summary>
    /// <param name="cancellationToken">Used to abort program start.</param>
    /// <returns>A <see cref="Task"/>.</returns>
    public Task WaitForStartAsync(CancellationToken cancellationToken);
}
