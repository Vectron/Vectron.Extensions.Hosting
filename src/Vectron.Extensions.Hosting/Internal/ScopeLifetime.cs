using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Vectron.Extensions.Hosting.Internal;

/// <summary>
/// Allows consumers to perform cleanup during a graceful shutdown.
/// </summary>
[DebuggerDisplay("ScopeStarted = {ScopeStarted.IsCancellationRequested}, " +
    "ScopeStopping = {ScopeStopping.IsCancellationRequested}, " +
    "ScopeStopped = {ScopeStopped.IsCancellationRequested}")]
public sealed class ScopeLifetime : IScopedHostScopeLifetime, IDisposable
{
    private readonly ILogger<ScopeLifetime> logger;
    private readonly CancellationTokenSource startedSource = new();
    private readonly CancellationTokenSource stoppedSource = new();
    private readonly CancellationTokenSource stoppingSource = new();
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScopeLifetime"/> class.
    /// </summary>
    /// <param name="logger">A <see cref="ILogger"/>.</param>
    public ScopeLifetime(ILogger<ScopeLifetime> logger) => this.logger = logger;

    /// <inheritdoc/>
    public CancellationToken ScopeStarted => startedSource.Token;

    /// <inheritdoc/>
    public CancellationToken ScopeStopped => stoppedSource.Token;

    /// <inheritdoc/>
    public CancellationToken ScopeStopping => stoppingSource.Token;

    /// <inheritdoc/>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        startedSource.Dispose();
        stoppedSource.Dispose();
        stoppingSource.Dispose();
        disposed = true;
    }

    /// <summary>
    /// Signals the ScopeStarted event and blocks until it completes.
    /// </summary>
    public void NotifyStarted()
    {
        try
        {
            ExecuteHandlers(startedSource);
        }
        catch (Exception ex)
        {
            logger.ScopeStartupException(ex);
        }
    }

    /// <summary>
    /// Signals the ScopeStopped event and blocks until it completes.
    /// </summary>
    public void NotifyStopped()
    {
        try
        {
            ExecuteHandlers(stoppedSource);
        }
        catch (Exception ex)
        {
            logger.ScopeStoppedException(ex);
        }
    }

    /// <summary>
    /// Signals the ScopeStopping event and blocks until it completes.
    /// </summary>
    public void StopScope()
    {
        // Lock on CTS to synchronize multiple calls to StopScope. This guarantees that the first
        // call to StopScope and its callbacks run to completion before subsequent calls to
        // StopScope, which will no-op since the first call already requested cancellation, get a
        // chance to execute.
        lock (stoppingSource)
        {
            try
            {
                ExecuteHandlers(stoppingSource);
            }
            catch (Exception ex)
            {
                logger.ScopeStoppingException(ex);
            }
        }
    }

    private static void ExecuteHandlers(CancellationTokenSource cancel)
    {
        // No operation if this is already cancelled
        if (cancel.IsCancellationRequested)
        {
            return;
        }

        // Run the cancellation token callbacks
        cancel.Cancel(throwOnFirstException: false);
    }
}
