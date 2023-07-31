namespace Vectron.Extensions.Hosting.Internal;

/// <summary>
/// Minimalistic lifetime that does nothing.
/// </summary>
internal sealed class NullLifetime : IScopedHostLifetime
{
    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task WaitForStartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
