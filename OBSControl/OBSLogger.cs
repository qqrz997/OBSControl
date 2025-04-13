using System;
using OBSWebsocketDotNet;

namespace OBSControl;

public class OBSLogger : IOBSLogger
{
    public OBSLoggerSettings LoggerSettings { get; set; } = OBSLoggerSettings.None;

    public void Log(string message, OBSLogLevel level)
    {
        Plugin.Log.Log(level.ToIPALogLevel(), "[OBSWebSocket] " + message);
    }

    public void Log(Exception ex, OBSLogLevel level)
    {
        var ipaLevel = level.ToIPALogLevel();
        Plugin.Log.Log(ipaLevel, "Exception in OBSWebSocket:");
        Plugin.Log.Log(ipaLevel, ex);
    }
}

public static class OBSLoggerExtensions
{
    public static IPA.Logging.Logger.Level ToIPALogLevel(this OBSLogLevel logLevel)
    {
        switch (logLevel)
        {
            case OBSLogLevel.Debug:
                return IPA.Logging.Logger.Level.Debug;
            case OBSLogLevel.Info:
                return IPA.Logging.Logger.Level.Info;
            case OBSLogLevel.Warning:
                return IPA.Logging.Logger.Level.Warning;
            case OBSLogLevel.Error:
                return IPA.Logging.Logger.Level.Error;
            default:
                return IPA.Logging.Logger.Level.Debug;
        }
    }
}