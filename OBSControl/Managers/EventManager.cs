using System;
using System.Collections.Generic;
using System.Linq;
using OBSControl.Utilities;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Communication;
using OBSWebsocketDotNet.Types;
using OBSWebsocketDotNet.Types.Events;
using Zenject;

namespace OBSControl.Managers;

internal class EventManager : IInitializable, IDisposable
{
    private readonly PluginConfig pluginConfig;
    private readonly IOBSWebsocket obsWebsocket;
    
    public EventManager(PluginConfig pluginConfig, IOBSWebsocket obsWebsocket)
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
    }

    public void Dispose()
    {
        obsWebsocket.Connected -= ObsConnected;
        obsWebsocket.Disconnected -= ObsDisconnected;
        obsWebsocket.RecordStateChanged -= ObsRecordStateChanged;
        obsWebsocket.StreamStateChanged -= ObsStreamStateChanged;
        obsWebsocket.SceneListChanged -= ObsSceneListChanged;
        obsWebsocket.CurrentProgramSceneChanged -= ObsCurrentProgramSceneChanged;
    }

    private void ObsConnected(object sender, EventArgs e)
    {
        Plugin.Log.Info($"OBS Connected to {pluginConfig.GetFullAddress()}.");
        ConnectionStateChanged?.Invoke(true);
        UpdateSceneList();
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
        UpdateSceneList();
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

    private void UpdateSceneList()
    {
        if (!obsWebsocket.IsConnected)
        {
            return;
        }
        
        try
        {
            var sceneNames = obsWebsocket.GetSceneList().Scenes.Select(s => s.Name);
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