using System.Diagnostics;
using System.Runtime.ExceptionServices;
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
internal sealed class ScopedHost : IScopedHost
{
    private readonly ILogger<ScopedHost> logger;
    private readonly ScopedHostOptions options;
    private readonly IScopedHostLifetime scopedHostLifetime;
    private readonly ScopedHostScopeLifetime scopedHostScopeLifetime;
    private IEnumerable<IScopedHostedLifecycleService>? hostedLifecycleServices;
    private IEnumerable<IScopedHostedService>? hostedServices;
    private bool hostStarting;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScopedHost"/> class.
    /// </summary>
    /// <param name="services">The <see cref="IServiceProvider"/> for creating services.</param>
    /// <param name="scopedHostScopeLifetime">The scope lifetime.</param>
    /// <param name="logger">A <see cref="ILogger"/>.</param>
    /// <param name="scopedHostLifetime">A <see cref="IScopedHostLifetime"/>.</param>
    /// <param name="options">The <see cref="ScopedHostOptions"/> for this host.</param>
    public ScopedHost(
        IServiceProvider services,
        IScopedHostScopeLifetime scopedHostScopeLifetime,
        ILogger<ScopedHost> logger,
        IScopedHostLifetime scopedHostLifetime,
        IOptions<ScopedHostOptions> options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(scopedHostLifetime);

        Services = services;
        this.scopedHostScopeLifetime = (scopedHostScopeLifetime as ScopedHostScopeLifetime)
            ?? throw new ArgumentException("Replacing IScopedHostScopeLifetime is not supported.", nameof(scopedHostScopeLifetime));
        this.logger = logger;
        this.scopedHostLifetime = scopedHostLifetime;
        this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    public IServiceProvider Services
    {
        get;
    }

    /// <summary>
    /// Order: IHostLifetime.WaitForStartAsync IHostedLifecycleService.StartingAsync
    /// IHostedService.Start IHostedLifecycleService.StartedAsync IScopedHostScopeLifetime.Started.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> for stopping the startup.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        logger.Starting();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (options.StartupTimeout != Timeout.InfiniteTimeSpan)
        {
            cts.CancelAfter(options.StartupTimeout);
        }

        cancellationToken = cts.Token;

        // This may not catch exceptions.
        await scopedHostLifetime.WaitForStartAsync(cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        List<Exception> exceptions = [];
        hostedServices ??= Services.GetRequiredService<IEnumerable<IScopedHostedService>>();
        hostedLifecycleServices = GetHostLifecycles(hostedServices);
        hostStarting = true;
        var concurrent = options.ServicesStartConcurrently;
        var abortOnFirstException = !concurrent;

        // Call StartingAsync().
        if (hostedLifecycleServices is not null)
        {
            await ForeachServiceAsync(
                hostedLifecycleServices,
                concurrent,
                abortOnFirstException,
                exceptions,
                (service, token) => service.StartingAsync(token),
                cancellationToken)
                .ConfigureAwait(false);

            // Exceptions in StartingAsync cause startup to be aborted.
            LogAndRethrow();
        }

        // Call StartAsync().
        await ForeachServiceAsync(
            hostedServices,
            concurrent,
            abortOnFirstException,
            exceptions,
            async (service, token) => await service.StartAsync(token).ConfigureAwait(false),
            cancellationToken)
            .ConfigureAwait(false);

        // Call StartedAsync().
        if (hostedLifecycleServices is not null)
        {
            await ForeachServiceAsync(
                hostedLifecycleServices,
                concurrent,
                abortOnFirstException,
                exceptions,
                (service, token) => service.StartedAsync(token),
                cancellationToken)
                .ConfigureAwait(false);
        }

        // Exceptions in StartedAsync cause startup to be aborted.
        LogAndRethrow();

        // Call IScopedHostScopeLifetime.Started This catches all exceptions and does not re-throw.
        scopedHostScopeLifetime.NotifyStarted();

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

        logger.Started();
    }

    /// <summary>
    /// Order: IScopedScopeLifetime.ScopeStopping; IHostedLifecycleService.StoppingAsync
    /// IScopedHostScopeLifetime.ScopeStopping IHostedService.Stop
    /// IHostedLifecycleService.StoppedAsync IHostLifetime.StopAsync IScopedHostScopeLifetime.Stopped.
    /// </summary>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> to cancel the graceful stopping.
    /// </param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        logger.Stopping();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (options.ShutdownTimeout != Timeout.InfiniteTimeSpan)
        {
            cts.CancelAfter(options.ShutdownTimeout);
        }

        cancellationToken = cts.Token;

        List<Exception> exceptions = [];
        if (!hostStarting)
        {
            // Call IScopedScopeLifetime.ScopeStopping. This catches all exceptions and does not re-throw.
            scopedHostScopeLifetime.StopScope();
        }
        else
        {
            Debug.Assert(hostedServices != null, "Hosted services are resolved when host is started.");

            // Ensure hosted services are stopped in LIFO order
            var reversedServices = hostedServices.Reverse();
            var reversedLifetimeServices = hostedLifecycleServices?.Reverse();
            var concurrent = options.ServicesStopConcurrently;

            // Call StoppingAsync().
            if (reversedLifetimeServices is not null)
            {
                await ForeachServiceAsync(
                    reversedLifetimeServices,
                    concurrent,
                    abortOnFirstException: false,
                    exceptions: exceptions,
                    operation: (service, token) => service.StoppingAsync(token),
                    token: cancellationToken)
                    .ConfigureAwait(false);
            }

            // Call IScopedHostScopeLifetime.ScopeStopping. This catches all exceptions and does not re-throw.
            scopedHostScopeLifetime.StopScope();

            // Call StopAsync().
            await ForeachServiceAsync(
                reversedServices,
                concurrent,
                abortOnFirstException: false,
                exceptions,
                (service, token) => service.StopAsync(token),
                cancellationToken)
                .ConfigureAwait(false);

            // Call StoppedAsync().
            if (reversedLifetimeServices is not null)
            {
                await ForeachServiceAsync(
                    reversedLifetimeServices,
                    concurrent,
                    abortOnFirstException: false,
                    exceptions,
                    (service, token) => service.StoppedAsync(token),
                    cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        // Call IScopedHostScopeLifetime.Stopped This catches all exceptions and does not re-throw.
        scopedHostScopeLifetime.NotifyStopped();

        // This may not catch exceptions, so we do it here.
        try
        {
            await scopedHostLifetime.StopAsync(cancellationToken).ConfigureAwait(false);
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

        logger.Stopped();
    }

    private static async Task ForeachServiceAsync<T>(
            IEnumerable<T> services,
            bool concurrent,
            bool abortOnFirstException,
            List<Exception> exceptions,
            Func<T, CancellationToken, Task> operation,
            CancellationToken token)
    {
        if (concurrent)
        {
            // The beginning synchronous portions of the implementations are run serially in
            // registration order for performance since it is common to return Task.Completed as a
            // noop. Any subsequent asynchronous portions are grouped together and run concurrently.
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
                    tasks.Add(task);
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
                    if (groupedTasks.IsFaulted
                        && groupedTasks.Exception != null)
                    {
                        exceptions.AddRange(groupedTasks.Exception.InnerExceptions);
                    }
                    else
                    {
                        exceptions.Add(ex);
                    }
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
