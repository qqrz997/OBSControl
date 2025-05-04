using System;
using System.Threading.Tasks;
using OBSControl.Utilities;
using OBSWebsocketDotNet;
using Zenject;

namespace OBSControl.Managers;

internal class ConnectionManager : IInitializable, IDisposable
{
    private readonly PluginConfig pluginConfig;
    private readonly IOBSWebsocket obsWebsocket;

    public ConnectionManager(PluginConfig pluginConfig, IOBSWebsocket obsWebsocket)
    {
        this.pluginConfig = pluginConfig;
        this.obsWebsocket = obsWebsocket;
    }

    public void Initialize()
    {
        Task.Run(() => RepeatConnect(attempts: 3, interval: 5000));
    }

    public void Dispose()
    {
        if (obsWebsocket.IsConnected) obsWebsocket.Disconnect();
    }

    public void ToggleConnect()
    {
        if (obsWebsocket.IsConnected) obsWebsocket.Disconnect();
        else Connect();
    }
    
    private async Task RepeatConnect(int attempts, int interval)
    {
        try
        {
            if (!pluginConfig.AddressIsValid())
            {
                Plugin.Log.Error("Server address or port is invalid. Unable to connect to OBS.");
                return;
            }
            Plugin.Log.Info("Repeatedly attempting to connect to OBS websocket server.");
            while (attempts-- > 0 && !obsWebsocket.IsConnected)
            {
                Connect();
                await Task.Delay(interval);
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error in RepeatTryConnect: {ex.Message}");
            Plugin.Log.Debug(ex);
        }
    }

    private void Connect()
    {
        if (obsWebsocket.IsConnected)
        {
            Plugin.Log.Info("Tried connecting when OBS is already connected.");
            return;
        }
        
        if(!pluginConfig.AddressIsValid())
        {
            Plugin.Log.Error("Server address or port is invalid.");
            return;
        }

        var serverAddress = pluginConfig.GetFullAddress();
        try
        {
            obsWebsocket.ConnectAsync(serverAddress, pluginConfig.ServerPassword);
            Plugin.Log.Info($"Finished attempting to connect to {serverAddress}");
        }
        catch (AuthFailureException)
        {
            Plugin.Log.Info($"Authentication failed connecting to server {serverAddress}.");
        }
        catch (Exception ex)
        {
            Plugin.Log.Warn($"Failed to connect to server {serverAddress}: {ex.Message}.");
            Plugin.Log.Debug(ex);
        }
    }
}