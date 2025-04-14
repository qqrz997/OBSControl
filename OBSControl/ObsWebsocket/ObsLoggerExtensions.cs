using IPA.Logging;
using OBSWebsocketDotNet;

namespace OBSControl.ObsWebsocket;

public static class ObsLoggerExtensions
{
    public static Logger.Level ToIpaLogLevel(this OBSLogLevel logLevel) => logLevel switch
    {
        OBSLogLevel.Debug => Logger.Level.Debug,
        OBSLogLevel.Info => Logger.Level.Info,
        OBSLogLevel.Warning => Logger.Level.Warning,
        OBSLogLevel.Error => Logger.Level.Error,
        _ => Logger.Level.Debug
    };
}