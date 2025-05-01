namespace OBSControl.Utilities;

internal static class ConfigExtensions
{
    public static bool AddressIsValid(this PluginConfig config) =>
        !string.IsNullOrEmpty(config.WsIpAddress) && !string.IsNullOrEmpty(config.WsPort);

    public static string GetFullAddress(this PluginConfig config) =>
        $"ws://{config.WsIpAddress}:{config.WsPort}";
}