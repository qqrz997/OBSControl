using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Communication;
using OBSWebsocketDotNet.Types;
using OBSWebsocketDotNet.Types.Events;
using Zenject;

namespace OBSControl.Managers;

internal class ObsManager : IInitializable, IDisposable
{
    private readonly PluginConfig pluginConfig;
    private readonly List<string> sceneNames = [];
    
    public ObsManager(PluginConfig pluginConfig)
    {
        this.pluginConfig = pluginConfig;
        
        Obs = new OBSWebsocket();
    }

    public event Action<bool>? ConnectionStateChanged; 
    public event Action<string>? SceneChanged; 
    public event Action<IEnumerable<string>>? SceneNamesUpdated; 
    public event Action<OutputState>? RecordingStateChanged;
    public event Action<OutputState>? StreamingStateChanged;
    // public event Action<HeartBeatEventArgs>? HeartBeatChanged;
    
    public IOBSWebsocket Obs { get; }

    public bool IsConnected { get; private set; }
    public string CurrentScene { get; private set; } = "Unknown";
    public IEnumerable<string> SceneNames => sceneNames;
    public OutputState RecordingState { get; private set; } = OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED;
    public OutputState StreamingState { get; private set; } = OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED;
    // public StreamStatusEventArgs? StreamStatus { get; private set; }
    // public HeartBeatEventArgs? HeartBeat { get; private set; }

    public void Initialize()
    {
        Obs.Connected += ObsConnected;
        Obs.Disconnected += ObsDisconnected;
        IsConnected = Obs.IsConnected;
        Obs.RecordStateChanged += ObsRecordStateChanged;
        Obs.StreamStateChanged += ObsStreamStateChanged;
        Obs.SceneListChanged += ObsSceneListChanged;
        // Obs.Heartbeat += ObsHeartBeat;
        Obs.CurrentProgramSceneChanged += ObsCurrentProgramSceneChanged;
        
        Task.Run(() => RepeatTryConnect(3, 5000));
    }

    public void Dispose()
    {
        Obs.Connected -= ObsConnected;
        Obs.Disconnected -= ObsDisconnected;
        Obs.RecordStateChanged -= ObsRecordStateChanged;
        Obs.StreamStateChanged -= ObsStreamStateChanged;
        Obs.SceneListChanged -= ObsSceneListChanged;
        // Obs.Heartbeat -= ObsHeartBeat;
        Obs.CurrentProgramSceneChanged -= ObsCurrentProgramSceneChanged;
        
        if (Obs.IsConnected)
        {
            Obs.Disconnect();
        }
    }

    private string? lastTryConnectMessage;
    public bool TryConnect()
    {
        var serverAddress = pluginConfig.ServerAddress;
        if(string.IsNullOrEmpty(serverAddress))
        {
            Plugin.Log.Error("ServerAddress cannot be null or empty.");
            return false;
        }
        
        if (Obs.IsConnected)
        {
            Plugin.Log.Info("TryConnect: OBS is already connected.");
            return true;
        }

        string message;
        try
        {
            Obs.ConnectAsync(serverAddress!, pluginConfig.ServerPassword);
            message = $"Finished attempting to connect to {pluginConfig.ServerAddress}";
            if (message != lastTryConnectMessage)
            {
                Plugin.Log.Info(message);
                lastTryConnectMessage = message;
            }
        }
        catch (AuthFailureException)
        {
            message = $"Authentication failed connecting to server {pluginConfig.ServerAddress}.";
            if (message != lastTryConnectMessage)
            {
                Plugin.Log.Info(message);
                lastTryConnectMessage = message;
            }

            return false;
        }
        catch (ErrorResponseException ex)
        {
            message = $"Failed to connect to server {pluginConfig.ServerAddress}: {ex.Message}.";
            if (message != lastTryConnectMessage)
            {
                Plugin.Log.Info(message);
                lastTryConnectMessage = message;
            }

            Plugin.Log.Debug(ex);
            return false;
        }
        catch (Exception ex)
        {
            message = $"Failed to connect to server {pluginConfig.ServerAddress}: {ex.Message}.";
            if (message != lastTryConnectMessage)
            {
                Plugin.Log.Info(message);
                Plugin.Log.Debug(ex);
                lastTryConnectMessage = message;
            }

            return false;
        }

        if (Obs.IsConnected)
            Plugin.Log.Info($"Connected to OBS @ {pluginConfig.ServerAddress}");

        return Obs.IsConnected;
    }

    private async Task RepeatTryConnect(int attempts, int intervalMilliseconds)
    {
        try
        {
            if (string.IsNullOrEmpty(pluginConfig.ServerAddress))
            {
                Plugin.Log.Error("The ServerAddress in the config is null or empty. Unable to connect to OBS.");
                return;
            }
            
            Plugin.Log.Info($"Attempting to connect to {pluginConfig.ServerAddress}");

            while (attempts-- > 0 && !TryConnect())
            {
                await Task.Delay(intervalMilliseconds);
            }

            if (Obs.IsConnected)
            {
                Plugin.Log.Info($"OBS {Obs.GetVersion().OBSStudioVersion} is connected.");
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
        if (IsConnected)
        {
            return;
        }
        Plugin.Log.Info("OBS Connected.");
        IsConnected = Obs.IsConnected;
        ConnectionStateChanged?.Invoke(true);
        TryFetchSceneList();
    }
    
    private void ObsDisconnected(object sender, ObsDisconnectionInfo disconnectionInfo)
    {
        if (!IsConnected)
        {
            return;
        }
        
        Plugin.Log.Info("OBS Disconnected.");
        IsConnected = Obs.IsConnected;
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

    // private void ObsHeartBeat(object sender, HeartBeatEventArgs e)
    // {
    //     HeartBeat = e;
    //     HeartBeatChanged?.Invoke(e);
    // }

    private void TryFetchSceneList()
    {
        try
        {
            sceneNames.Clear();
            sceneNames.AddRange(Obs.GetSceneList().Scenes.Select(s => s.Name));
            SceneNamesUpdated?.Invoke(sceneNames);
            
            CurrentScene = Obs.GetCurrentProgramScene();
            SceneChanged?.Invoke(CurrentScene);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error getting scene list: {ex.Message}");
            Plugin.Log.Debug(ex);
        }
    }
}