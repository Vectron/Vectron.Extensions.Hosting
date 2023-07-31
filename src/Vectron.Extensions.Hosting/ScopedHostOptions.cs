using Microsoft.Extensions.Hosting;

namespace Vectron.Extensions.Hosting;

/// <summary>
/// Options for <see cref="IScopedHost"/>.
/// </summary>
public class ScopedHostOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScopedHostOptions"/> class.
    /// </summary>
    public ScopedHostOptions() => BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost;

    /// <summary>
    /// Gets or sets the behavior the <see cref="IScopedHost"/> will follow when any of its
    /// <see cref="BackgroundService"/> instances throw an unhandled exception.
    /// </summary>
    /// <remarks>Defaults to <see cref="BackgroundServiceExceptionBehavior.StopHost"/>.</remarks>
    public BackgroundServiceExceptionBehavior BackgroundServiceExceptionBehavior
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the <see cref="IScopedHost"/> will start registered
    /// instances of <see cref="IScopedHostedService"/> concurrently or sequentially. Defaults to false.
    /// </summary>
    public bool ServicesStartConcurrently
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the <see cref="IScopedHost"/> will stop registered
    /// instances of <see cref="IHostedService"/> concurrently or sequentially. Defaults to false.
    /// </summary>
    public bool ServicesStopConcurrently
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the default timeout for <see cref="IScopedHost.StopAsync(CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// This timeout also encompasses all host services implementing
    /// <see cref="IScopedHostedLifecycleService.StoppingAsync(CancellationToken)"/> and <see cref="IScopedHostedLifecycleService.StoppedAsync(CancellationToken)"/>.
    /// </remarks>
    public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the default timeout for <see cref="IHost.StartAsync(CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// This timeout also encompasses all host services implementing
    /// <see cref="IScopedHostedLifecycleService.StartingAsync(CancellationToken)"/> and <see cref="IScopedHostedLifecycleService.StartedAsync(CancellationToken)"/>.
    /// </remarks>
    public TimeSpan StartupTimeout { get; set; } = Timeout.InfiniteTimeSpan;
}
