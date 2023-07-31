using Microsoft.Extensions.Logging;

namespace Vectron.Extensions.Hosting;

/// <summary>
/// The logging part for the scoped host.
/// </summary>
internal static partial class ScopedHostLoggingExtensions
{
    [LoggerMessage(EventId = 9, Level = LogLevel.Error, Message = "BackgroundService failed")]
    public static partial void BackgroundServiceFaulted(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 10, Level = LogLevel.Critical, Message = "The HostOptions.BackgroundServiceExceptionBehavior is configured to StopHost. A BackgroundService has thrown an unhandled exception, and the IHost instance is stopping. To avoid this behavior, configure this to Ignore; however the BackgroundService will not be restarted.")]
    public static partial void BackgroundServiceStoppingHost(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 11, Level = LogLevel.Error, Message = "Hosting failed to start")]
    public static partial void HostedServiceStartupFaulted(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 6, Level = LogLevel.Error, Message = "An error occurred starting the application")]
    public static partial void ScopeStartupException(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 8, Level = LogLevel.Error, Message = "An error occurred stopping the scope")]
    public static partial void ScopeStoppedException(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 7, Level = LogLevel.Error, Message = "An error occurred stopping the scope")]
    public static partial void ScopeStoppingException(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Hosting started")]
    public static partial void Started(this ILogger logger);

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Scoped hosting starting")]
    public static partial void Starting(this ILogger logger);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Hosting stopped")]
    public static partial void Stopped(this ILogger logger);

    [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "Hosting shutdown exception")]
    public static partial void StoppedWithException(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "Hosting stopping")]
    public static partial void Stopping(this ILogger logger);
}
