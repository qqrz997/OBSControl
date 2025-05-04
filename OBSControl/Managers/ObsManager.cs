using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OBSControl.Utilities;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Communication;
using OBSWebsocketDotNet.Types;
using OBSWebsocketDotNet.Types.Events;
using Zenject;

namespace OBSControl.Managers;

internal class ObsManager : IInitializable, IDisposable
{
    private readonly PluginConfig pluginConfig;
    private readonly IOBSWebsocket obsWebsocket;
    private readonly List<string> sceneNames = [];
    
    public ObsManager(PluginConfig pluginConfig, IOBSWebsocket obsWebsocket)
    {
        this.pluginConfig = pluginConfig;
        this.obsWebsocket = obsWebsocket;
    }

    public event Action<bool>? ConnectionStateChanged; 
    public event Action<string>? SceneChanged; 
    public event Action<IEnumerable<string>>? SceneNamesUpdated; 
    public event Action<OutputState>? RecordingStateChanged;
    public event Action<OutputState>? StreamingStateChanged;

    public string CurrentScene { get; private set; } = "Unknown";
    public IEnumerable<string> SceneNames => sceneNames;
    public OutputState RecordingState { get; private set; } = OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED;
    public OutputState StreamingState { get; private set; } = OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED;

    public void Initialize()
    {
        obsWebsocket.Connected += ObsConnected;
        obsWebsocket.Disconnected += ObsDisconnected;
        obsWebsocket.RecordStateChanged += ObsRecordStateChanged;
        obsWebsocket.StreamStateChanged += ObsStreamStateChanged;
        obsWebsocket.SceneListChanged += ObsSceneListChanged;
        obsWebsocket.CurrentProgramSceneChanged += ObsCurrentProgramSceneChanged;
        
        Task.Run(() => RepeatTryConnect(3, 5000));
    }

    public void Dispose()
    {
        obsWebsocket.Connected -= ObsConnected;
        obsWebsocket.Disconnected -= ObsDisconnected;
        obsWebsocket.RecordStateChanged -= ObsRecordStateChanged;
        obsWebsocket.StreamStateChanged -= ObsStreamStateChanged;
        obsWebsocket.SceneListChanged -= ObsSceneListChanged;
        obsWebsocket.CurrentProgramSceneChanged -= ObsCurrentProgramSceneChanged;
        
        if (obsWebsocket.IsConnected)
        {
            obsWebsocket.Disconnect();
        }
    }

    public void ToggleConnect()
    {
        if (obsWebsocket.IsConnected)
        {
            obsWebsocket.Disconnect();
        }
        else
        {
            TryConnect();
        }
    }

    private void TryConnect()
    {
        if (obsWebsocket.IsConnected)
        {
            Plugin.Log.Info("TryConnect: OBS is already connected.");
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

    private async Task RepeatTryConnect(int attempts, int intervalMilliseconds)
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
                TryConnect();
                await Task.Delay(intervalMilliseconds);
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error in RepeatTryConnect: {ex.Message}");
            Plugin.Log.Debug(ex);
        }
    }

    private void ObsConnected(object sender, EventArgs e)
    {
        Plugin.Log.Info($"OBS Connected to {pluginConfig.GetFullAddress()}.");
        ConnectionStateChanged?.Invoke(true);
        TryFetchSceneList();
    }
    
    private void ObsDisconnected(object sender, ObsDisconnectionInfo disconnectionInfo)
    {
        Plugin.Log.Info($"OBS Disconnected: {disconnectionInfo.WebsocketDisconnectionInfo.CloseStatusDescription}");
        ConnectionStateChanged?.Invoke(false);
    }

    private void ObsCurrentProgramSceneChanged(object sender, ProgramSceneChangedEventArgs e)
    {
        CurrentScene = e.SceneName;
        SceneChanged?.Invoke(e.SceneName);
    }

    private void ObsSceneListChanged(object sender, SceneListChangedEventArgs e)
    {
        TryFetchSceneList();
    }

    private void ObsRecordStateChanged(object sender, RecordStateChangedEventArgs e)
    {
        Plugin.Log.Info($"Recording State Changed: {e.OutputState.State}");
        RecordingState = e.OutputState.State;
        RecordingStateChanged?.Invoke(e.OutputState.State);
    }

    private void ObsStreamStateChanged(object sender, StreamStateChangedEventArgs e)
    {
        Plugin.Log.Info($"Streaming State Changed: {e.OutputState.State}");
        StreamingState = e.OutputState.State;
        StreamingStateChanged?.Invoke(e.OutputState.State);
    }

    private void TryFetchSceneList()
    {
        if (!obsWebsocket.IsConnected)
        {
            return;
        }
        
        try
        {
            sceneNames.Clear();
            sceneNames.AddRange(obsWebsocket.GetSceneList().Scenes.Select(s => s.Name));
            SceneNamesUpdated?.Invoke(sceneNames);
            
            CurrentScene = obsWebsocket.GetCurrentProgramScene();
            SceneChanged?.Invoke(CurrentScene);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error getting scene list: {ex.Message}");
            Plugin.Log.Debug(ex);
        }
    }
}