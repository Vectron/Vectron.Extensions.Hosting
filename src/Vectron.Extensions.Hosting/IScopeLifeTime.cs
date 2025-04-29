namespace Vectron.Extensions.Hosting;

/// <summary>
/// Events for when new Dependency injection scopes are created or destroyed.
/// </summary>
public interface IScopeLifeTime
{
    /// <summary>
    /// An event that is triggered when a scope is created.
    /// </summary>
    public event EventHandler<ScopeChangedEventArgs>? ScopeCreated;

    /// <summary>
    /// An event that is triggered when a scope is being destroyed.
    /// </summary>
    public event EventHandler<ScopeChangedEventArgs>? ScopeDestroying;
}
