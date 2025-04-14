using System;
using OBSWebsocketDotNet;

namespace OBSControl.ObsWebsocket;

public class ObsLogger : IOBSLogger
{
    public OBSLoggerSettings LoggerSettings { get; set; } = OBSLoggerSettings.None;

    public void Log(string message, OBSLogLevel level)
    {
        Plugin.Log.Log(level.ToIpaLogLevel(), "[OBSWebSocket] " + message);
    }

    public void Log(Exception ex, OBSLogLevel level)
    {
        var ipaLevel = level.ToIpaLogLevel();
        Plugin.Log.Log(ipaLevel, "Exception in OBSWebSocket:");
        Plugin.Log.Log(ipaLevel, ex);
    }
}