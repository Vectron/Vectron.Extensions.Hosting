using Microsoft.Extensions.DependencyInjection;

namespace Vectron.Extensions.Hosting;

/// <summary>
/// Extension methods for <see cref="IScopedHost"/>.
/// </summary>
public static class ScopedHostExtensions
{
    /// <summary>
    /// Runs an scoped host and returns a <see cref="Task"/> that only completes when the token is
    /// triggered or shutdown is triggered. The <paramref name="host"/> instance is disposed of
    /// after running.
    /// </summary>
    /// <param name="host">The <see cref="IScopedHost"/> to run.</param>
    /// <param name="token">The token to trigger shutdown.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    public static async Task RunAsync(this IScopedHost host, CancellationToken token = default)
    {
        await host.StartAsync(token).ConfigureAwait(false);
        await host.WaitForShutdownAsync(token).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns a Task that completes when shutdown is triggered via the given token.
    /// </summary>
    /// <param name="host">The running <see cref="IScopedHost"/>.</param>
    /// <param name="token">The token to trigger shutdown.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    public static async Task WaitForShutdownAsync(this IScopedHost host, CancellationToken token = default)
    {
        var scopeLifetime = host.Services.GetRequiredService<IScopedHostScopeLifetime>();
        var tokenRegistration = token.Register(state => ((IScopedHostScopeLifetime)state!).StopScope(), scopeLifetime);
        await using (tokenRegistration.ConfigureAwait(false))
        {
            var waitForStop = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var stoppingTokenRegistration = scopeLifetime.ScopeStopping.Register(
                obj =>
                {
                    var tcs = (TaskCompletionSource<object?>)obj!;
                    _ = tcs.TrySetResult(null);
                },
                waitForStop);

            _ = await waitForStop.Task.ConfigureAwait(false);

            // Host will use its default ShutdownTimeout if none is specified. The cancellation
            // token may have been triggered to unblock waitForStop. Don't pass it here because that
            // would trigger an abortive shutdown.
            await host.StopAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }
}
