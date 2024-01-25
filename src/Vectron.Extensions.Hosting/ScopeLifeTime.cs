using Microsoft.Extensions.DependencyInjection;

namespace Vectron.Extensions.Hosting;

/// <summary>
/// Implementation of <see cref="IScopeLifeTime"/>.
/// </summary>
internal sealed class ScopeLifeTime : IScopeLifeTime
{
    /// <inheritdoc/>
    public event EventHandler<ScopeChangedEventArgs>? ScopeCreated;

    /// <inheritdoc/>
    public event EventHandler<ScopeChangedEventArgs>? ScopeDestroying;

    /// <summary>
    /// Signals the <see cref="ScopeCreated"/> event and blocks until it completes.
    /// </summary>
    /// <param name="scope">The scoped <see cref="IServiceProvider"/>.</param>
    public void NotifyCreated(IServiceScope scope)
        => ScopeCreated?.Invoke(this, new ScopeChangedEventArgs(scope));

    /// <summary>
    /// Signals the <see cref="ScopeDestroying"/> event and blocks until it completes.
    /// </summary>
    /// <param name="scope">The scoped <see cref="IServiceProvider"/>.</param>
    public void NotifyDestroying(IServiceScope scope)
        => ScopeDestroying?.Invoke(this, new ScopeChangedEventArgs(scope));
}
