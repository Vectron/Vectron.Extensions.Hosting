using Microsoft.Extensions.DependencyInjection;

namespace Vectron.Extensions.Hosting;

/// <summary>
/// <see cref="EventArgs"/> for the <see cref="IScopeLifeTime.ScopeCreated"/> and <see cref="IScopeLifeTime.ScopeDestroying"/>.
/// </summary>
/// <param name="scope">The changed <see cref="IServiceScope"/> scope.</param>
public sealed class ScopeChangedEventArgs(IServiceScope scope) : EventArgs
{
    /// <summary>
    /// Gets the scoped <see cref="IServiceScope"/>.
    /// </summary>
    public IServiceScope Scope => scope;
}
