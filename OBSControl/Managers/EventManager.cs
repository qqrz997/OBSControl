using System;
using System.Collections.Generic;
using System.IO;
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
    public event Action<long>? DriveSpaceUpdated;

    public string CurrentScene { get; private set; } = "Unknown";
    public long DriveSpace { get; private set; } = 0;
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
        UpdateAvailableDriveSpace();
        UpdateRecordingState();
        UpdateStreamingState();
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

        if (e.OutputState.State == OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED)
        {
            UpdateAvailableDriveSpace();
        }
    }

    private void ObsStreamStateChanged(object sender, StreamStateChangedEventArgs e)
    {
        Plugin.Log.Info($"Streaming State Changed: {e.OutputState.State}");
        StreamingState = e.OutputState.State;
        StreamingStateChanged?.Invoke(e.OutputState.State);
    }

    private void UpdateSceneList()
    {
        if (!obsWebsocket.IsConnected) return;
        
        try
        {
            var sceneNames = obsWebsocket.GetSceneList().Scenes.Select(s => s.Name);
            SceneNamesUpdated?.Invoke(sceneNames);
            
            CurrentScene = obsWebsocket.GetCurrentProgramScene();
            SceneChanged?.Invoke(CurrentScene);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Encountered a problem while trying to update scene list: {ex}");
        }
    }

    private void UpdateAvailableDriveSpace()
    {
        if (!obsWebsocket.IsConnected) return;

        try
        {
            var recordPath = obsWebsocket.GetRecordDirectory();
            var recordDirectory = new DirectoryInfo(recordPath);
            var driveInfo = new DriveInfo(recordDirectory.Root.FullName);

            DriveSpace = driveInfo.AvailableFreeSpace;
            DriveSpaceUpdated?.Invoke(driveInfo.AvailableFreeSpace);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Encountered a problem while trying to update available drive space: {ex}");
        }
    }

    private void UpdateRecordingState()
    {
        RecordingState = obsWebsocket.GetRecordStatus().IsRecording ? OutputState.OBS_WEBSOCKET_OUTPUT_STARTED : OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED;
        RecordingStateChanged?.Invoke(RecordingState);
    }

    private void UpdateStreamingState()
    {
        StreamingState = obsWebsocket.GetStreamStatus().IsActive ? OutputState.OBS_WEBSOCKET_OUTPUT_STARTED : OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED;
        StreamingStateChanged?.Invoke(StreamingState);
    }
}