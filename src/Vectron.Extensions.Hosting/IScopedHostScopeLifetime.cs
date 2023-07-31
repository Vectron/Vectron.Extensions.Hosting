namespace Vectron.Extensions.Hosting;

/// <summary>
/// Allows consumers to be notified of scope lifetime events.
/// </summary>
public interface IScopedHostScopeLifetime
{
    /// <summary>
    /// Gets a <see cref="CancellationToken"/> that is triggered when the scope host has fully started.
    /// </summary>
    CancellationToken ScopeStarted
    {
        get;
    }

    /// <summary>
    /// Gets a <see cref="CancellationToken"/> that is Triggered when the scope host has completed a
    /// graceful shutdown. The scope will not exit until all callbacks registered on this token have completed.
    /// </summary>
    CancellationToken ScopeStopped
    {
        get;
    }

    /// <summary>
    /// Gets a <see cref="CancellationToken"/> that is Triggered when the scope host is starting a
    /// graceful shutdown. Shutdown will block until all callbacks registered on this token have completed.
    /// </summary>
    CancellationToken ScopeStopping
    {
        get;
    }

    /// <summary>
    /// Requests termination of the current scope.
    /// </summary>
    void StopScope();
}
