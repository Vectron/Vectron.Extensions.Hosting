using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Vectron.Extensions.Hosting.Internal;

/// <summary>
/// Implementation of a <see cref="IScopedHost"/>.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "MA0051:Method is too long",
    Justification = "Code copied from https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Hosting/src/Internal/Host.cs")]
internal sealed partial class ScopedHost : IScopedHost, IAsyncDisposable
{
    private readonly ILogger<ScopedHost> logger;
    private readonly ScopedHostOptions options;
    private readonly IScopedHostLifetime scopedHostLifetime;
    private readonly ScopeLifetime scopeLifetime;
    private IEnumerable<IScopedHostedLifecycleService>? hostedLifecycleServices;
    private IEnumerable<IScopedHostedService>? hostedServices;
    private volatile bool stopCalled;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScopedHost"/> class.
    /// </summary>
    /// <param name="services">The <see cref="IServiceProvider"/> for creating services.</param>
    /// <param name="scopeLifetime">The scope lifetime.</param>
    /// <param name="logger">A <see cref="ILogger"/>.</param>
    /// <param name="scopedHostLifetime">A <see cref="IScopedHostLifetime"/>.</param>
    /// <param name="options">The <see cref="ScopedHostOptions"/> for this host.</param>
    public ScopedHost(
        IServiceProvider services,
        IScopedHostScopeLifetime scopeLifetime,
        ILogger<ScopedHost> logger,
        IScopedHostLifetime scopedHostLifetime,
        IOptions<ScopedHostOptions> options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(scopeLifetime);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(scopedHostLifetime);

        Services = services;
        this.scopeLifetime = (scopeLifetime as ScopeLifetime)!;
        if (scopeLifetime is null)
        {
            throw new ArgumentException("Replacing IScopedHostScopeLifetime is not supported.", nameof(scopeLifetime));
        }

        this.logger = logger;
        this.scopedHostLifetime = scopedHostLifetime;
        this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    public IServiceProvider Services
    {
        get;
    }

    /// <inheritdoc/>
    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    /// <inheritdoc/>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP007:Don't dispose injected",
        Justification = "The function is supposed to dispose the injected")]
    public async ValueTask DisposeAsync()
    {
        // Dispose the service provider
        await DisposeAsync(Services).ConfigureAwait(false);

        static async ValueTask DisposeAsync(object o)
        {
            switch (o)
            {
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    break;

                case IDisposable disposable:
                    disposable.Dispose();
                    break;

                default:
                    break;
            }
        }
    }

    /// <summary>
    /// Order: IHostLifetime.WaitForStartAsync (can abort chain)
    /// Services.GetService{IStartupValidator}().Validate() (can abort chain)
    /// IHostedLifecycleService.StartingAsync IHostedService.Start
    /// IHostedLifecycleService.StartedAsync IHostApplicationLifetime.ApplicationStarted.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> for stopping the startup.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        logger.Starting();
        CancellationTokenSource? cts = null;
        CancellationTokenSource linkedCts;
        if (options.StartupTimeout != Timeout.InfiniteTimeSpan)
        {
            cts = new CancellationTokenSource(options.StartupTimeout);
            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken, scopeLifetime.ScopeStopping);
        }
        else
        {
            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, scopeLifetime.ScopeStopping);
        }

        using (cts)
        using (linkedCts)
        {
            var token = linkedCts.Token;

            // This may not catch exceptions.
            await scopedHostLifetime.WaitForStartAsync(token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();

            List<Exception> exceptions = [];
            hostedServices = Services.GetRequiredService<IEnumerable<IScopedHostedService>>();
            hostedLifecycleServices = GetHostLifecycles(hostedServices);
            var concurrent = options.ServicesStartConcurrently;
            var abortOnFirstException = !concurrent;

            // Call StartingAsync().
            if (hostedLifecycleServices is not null)
            {
                await ForeachService(
                    hostedLifecycleServices,
                    concurrent,
                    abortOnFirstException,
                    exceptions,
                    (service, token) => service.StartingAsync(token),
                    token)
                    .ConfigureAwait(false);

                // We do not abort on exceptions from StartingAsync.
            }

            // Call StartAsync(). We do not abort on exceptions from StartAsync.
            await ForeachService(
                hostedServices,
                concurrent,
                abortOnFirstException,
                exceptions,
            async (service, token) => await service.StartAsync(token).ConfigureAwait(false),
                {
                    await service.StartAsync(token).ConfigureAwait(false);
                },
                token).ConfigureAwait(false);

            // Call StartedAsync(). We do not abort on exceptions from StartedAsync.
            if (hostedLifecycleServices is not null)
            {
                await ForeachService(
                    hostedLifecycleServices,
                    concurrent,
                    abortOnFirstException,
                    exceptions,
                    (service, token) => service.StartedAsync(token),
                    token)
                    .ConfigureAwait(false);
            }

            LogAndRethrow();

            // Call IScopedHostScopeLifetime.Started This catches all exceptions and does not re-throw.
            scopeLifetime.NotifyStarted();

            // Log and abort if there are exceptions.
            void LogAndRethrow()
            {
                if (exceptions.Count > 0)
                {
                    if (exceptions.Count == 1)
                    {
                        // Rethrow if it's a single error
                        var singleException = exceptions[0];
                        logger.HostedServiceStartupFaulted(singleException);
                        ExceptionDispatchInfo.Capture(singleException).Throw();
                    }
                    else
                    {
                        var ex = new AggregateException("One or more hosted services failed to start.", exceptions);
                        logger.HostedServiceStartupFaulted(ex);
                        throw ex;
                    }
                }
            }
        }

        logger.Started();
    }

    /// <summary>
    /// Order: IHostedLifecycleService.StoppingAsync IHostApplicationLifetime.ApplicationStopping
    /// IHostedService.Stop IHostedLifecycleService.StoppedAsync
    /// IHostApplicationLifetime.ApplicationStopped IHostLifetime.StopAsync.
    /// </summary>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> for aborting the stop process.
    /// </param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        stopCalled = true;
        logger.Stopping();

        CancellationTokenSource? cts = null;
        CancellationTokenSource linkedCts;
        if (options.ShutdownTimeout != Timeout.InfiniteTimeSpan)
        {
            cts = new CancellationTokenSource(options.ShutdownTimeout);
            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);
        }
        else
        {
            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        }

        using (cts)
        using (linkedCts)
        {
            var token = linkedCts.Token;
            List<Exception> exceptions = [];

            // Started?
            if (hostedServices is null)
            {
                // Call IHostApplicationLifetime.ApplicationStopping. This catches all exceptions
                // and does not re-throw.
                scopeLifetime.StopScope();
            }
            else
            {
                // Ensure hosted services are stopped in LIFO order
                var reversedServices = hostedServices.Reverse();
                var reversedLifetimeServices = hostedLifecycleServices?.Reverse();
                var concurrent = options.ServicesStopConcurrently;

                // Call StoppingAsync().
                if (reversedLifetimeServices is not null)
                {
                    await ForeachService(
                        reversedLifetimeServices,
                        concurrent,
                        abortOnFirstException: false,
                        exceptions: exceptions,
                        operation: (service, token) => service.StoppingAsync(token),
                        token: token)
                        .ConfigureAwait(false);
                }

                // Call IHostApplicationLifetime.ApplicationStopping. This catches all exceptions
                // and does not re-throw.
                scopeLifetime.StopScope();

                // Call StopAsync().
                await ForeachService(
                    reversedServices,
                    concurrent,
                    abortOnFirstException: false,
                    exceptions: exceptions,
                    operation: (service, token) => service.StopAsync(token),
                    token: token)
                    .ConfigureAwait(false);

                // Call StoppedAsync().
                if (reversedLifetimeServices is not null)
                {
                    await ForeachService(
                        reversedLifetimeServices,
                        concurrent,
                        abortOnFirstException: false,
                        exceptions: exceptions,
                        operation: (service, token) => service.StoppedAsync(token),
                        token: token)
                        .ConfigureAwait(false);
                }
            }

            // Call IHostApplicationLifetime.Stopped This catches all exceptions and does not re-throw.
            scopeLifetime.NotifyStopped();

            // This may not catch exceptions, so we do it here.
            try
            {
                await scopedHostLifetime.StopAsync(token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }

            if (exceptions.Count > 0)
            {
                if (exceptions.Count == 1)
                {
                    // Rethrow if it's a single error
                    var singleException = exceptions[0];
                    logger.StoppedWithException(singleException);
                    ExceptionDispatchInfo.Capture(singleException).Throw();
                }
                else
                {
                    var ex = new AggregateException("One or more hosted services failed to stop.", exceptions);
                    logger.StoppedWithException(ex);
                    throw ex;
                }
            }
        }

        logger.Stopped();
    }

    private static async Task ForeachService<T>(
        IEnumerable<T> services,
        bool concurrent,
        bool abortOnFirstException,
        List<Exception> exceptions,
        Func<T, CancellationToken, Task> operation,
        CancellationToken token)
    {
        if (concurrent)
        {
            // The beginning synchronous portions of the implementations are run serially in registration order for
            // performance since it is common to return Task.Completed as a noop.
            // Any subsequent asynchronous portions are grouped together and run concurrently.
            List<Task>? tasks = null;
            foreach (var service in services)
            {
                Task task;
                try
                {
                    task = operation(service, token);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex); // Log exception from sync method.
                    continue;
                }

                if (task.IsCompleted)
                {
                    if (task.Exception is not null)
                    {
                        exceptions.AddRange(task.Exception.InnerExceptions); // Log exception from async method.
                    }
                }
                else
                {
                    // The task encountered an await; add it to a list to run concurrently.
                    tasks ??= [];
                    tasks.Add(Task.Run(() => task, token));
                }
            }

            if (tasks is not null)
            {
                var groupedTasks = Task.WhenAll(tasks);

                try
                {
                    await groupedTasks.ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions.AddRange(groupedTasks.Exception?.InnerExceptions ?? new[] { ex }.AsEnumerable());
                }
            }
        }
        else
        {
            foreach (var service in services)
            {
                try
                {
                    await operation(service, token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    if (abortOnFirstException)
                    {
                        return;
                    }
                }
            }
        }
    }

    private static List<IScopedHostedLifecycleService>? GetHostLifecycles(IEnumerable<IScopedHostedService> hostedServices)
    {
        List<IScopedHostedLifecycleService>? result = null;
        foreach (var hostedService in hostedServices)
        {
            if (hostedService is IScopedHostedLifecycleService service)
            {
                result ??= [];
                result.Add(service);
            }
        }

        return result;
    }
}
