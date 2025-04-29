using Microsoft.Extensions.DependencyInjection;

namespace Vectron.Extensions.Hosting.Internal;

/// <summary>
/// Default implementation of <see cref="IScopedHostFactory"/>.
/// </summary>
/// <param name="scopeFactory">The <see cref="IServiceScopeFactory"/>.</param>
/// <param name="scopeLifeTime">The <see cref="IScopeLifeTime"/>.</param>
internal sealed class ScopedHostFactory(IServiceScopeFactory scopeFactory, IScopeLifeTime scopeLifeTime) : IScopedHostFactory
{
    private readonly ScopeLifeTime scopeLifeTime = scopeLifeTime as ScopeLifeTime
        ?? throw new InvalidOperationException("Replacing IScopeLifeTime is not supported.");

    /// <inheritdoc/>
    public async Task RunScopedHostAsync(SetupScopeFunc setupAction, CancellationToken cancellationToken)
    {
        var scope = scopeFactory.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            await setupAction(scope.ServiceProvider, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            scopeLifeTime.NotifyCreated(scope);
            cancellationToken.ThrowIfCancellationRequested();
            var host = scope.ServiceProvider.GetRequiredService<IScopedHost>();
            await host.RunAsync(cancellationToken).ConfigureAwait(false);
            scopeLifeTime.NotifyDestroying(scope);
        }
    }
}
