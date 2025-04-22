using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using Zenject;

namespace OBSControl.Managers;

internal class ObsManager : IInitializable, IDisposable
{
    private readonly PluginConfig pluginConfig;
    private readonly List<string> sceneNames = [];
    
    public ObsManager(PluginConfig pluginConfig)
    {
        this.pluginConfig = pluginConfig;
        
        Obs = new();
    }

    public event Action<bool>? ConnectionStateChanged; 
    public event Action<string>? SceneChanged; 
    public event Action<IEnumerable<string>>? SceneNamesUpdated; 
    public event Action<OutputState>? RecordingStateChanged;
    public event Action<OutputState>? StreamingStateChanged;
    public event Action<StreamStatusEventArgs>? StreamStatusChanged;
    public event Action<HeartBeatEventArgs>? HeartBeatChanged;
    
    public OBSWebsocket Obs { get; }

    public bool IsConnected { get; private set; }
    public string CurrentScene { get; private set; } = "Unknown";
    public IEnumerable<string> SceneNames => sceneNames;
    public OutputState RecordingState { get; private set; } = OutputState.Unknown;
    public OutputState StreamingState { get; private set; } = OutputState.Unknown;
    public StreamStatusEventArgs? StreamStatus { get; private set; }
    public HeartBeatEventArgs? HeartBeat { get; private set; }

    public void Initialize()
    {
        Obs.Connected += ObsConnected;
        Obs.Disconnected += ObsDisconnected;
        IsConnected = Obs.IsConnected;
        Obs.RecordingStateChanged += ObsRecordingStateChanged;
        Obs.StreamingStateChanged += ObsStreamingStateChanged;
        Obs.StreamStatus += ObsStreamStatusChanged;
        Obs.SceneListChanged += ObsSceneListChanged;
        Obs.Heartbeat += ObsHeartBeat;
        Obs.SceneChanged += ObsSceneChanged;
        
        Task.Run(() => RepeatTryConnect(3, 5000));
    }

    public void Dispose()
    {
        Obs.Connected -= ObsConnected;
        Obs.Disconnected -= ObsDisconnected;
        Obs.RecordingStateChanged -= ObsRecordingStateChanged;
        Obs.StreamingStateChanged -= ObsStreamingStateChanged;
        Obs.StreamStatus -= ObsStreamStatusChanged;
        Obs.SceneListChanged -= ObsSceneListChanged;
        Obs.Heartbeat -= ObsHeartBeat;
        Obs.SceneChanged -= ObsSceneChanged;
        
        if (Obs.IsConnected)
        {
            Obs.Disconnect();
        }
    }

    private string? lastTryConnectMessage;
    public async Task<bool> TryConnect()
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
            await Obs.Connect(serverAddress!, pluginConfig.ServerPassword).ConfigureAwait(false);
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

    public async Task ToggleStreaming()
    {
        if (!Obs.IsConnected) return;
        if ((await Obs.GetStreamingStatus()).IsStreaming)
        {
            await Obs.StopStreaming();
        }
        else
        {
            await Obs.StartStreaming();
        }
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

            while (attempts-- > 0 && !await TryConnect())
            {
                await Task.Delay(intervalMilliseconds);
            }

            if (Obs.IsConnected)
            {
                Plugin.Log.Info($"OBS {(await Obs.GetVersion()).OBSStudioVersion} is connected.");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error in RepeatTryConnect: {ex.Message}");
            Plugin.Log.Debug(ex);
        }
    }

    private async void ObsConnected(object sender, EventArgs e)
    {
        if (IsConnected)
        {
            return;
        }
        Plugin.Log.Info("OBS Connected.");
        IsConnected = true;
        ConnectionStateChanged?.Invoke(true);
        await TryFetchSceneList();
    }
    
    private void ObsDisconnected(object sender, EventArgs e)
    {
        if (!IsConnected)
        {
            return;
        }
        
        Plugin.Log.Info("OBS Disconnected.");
        IsConnected = false;
        ConnectionStateChanged?.Invoke(false);
    }

    private void ObsSceneChanged(object sender, SceneChangeEventArgs e)
    {
        CurrentScene = e.NewSceneName;
        SceneChanged?.Invoke(e.NewSceneName);
    }

    private async void ObsSceneListChanged(object sender, EventArgs e)
    {
        await TryFetchSceneList();
    }

    private void ObsRecordingStateChanged(object sender, OutputStateChangedEventArgs e)
    {
        Plugin.Log.Info($"Recording State Changed: {e.OutputState}");
        RecordingState = e.OutputState;
        RecordingStateChanged?.Invoke(e.OutputState);
    }

    private void ObsStreamingStateChanged(object sender, OutputStateChangedEventArgs e)
    {
        Plugin.Log.Info($"Streaming State Changed: {e.OutputState}");
        StreamingState = e.OutputState;
        StreamingStateChanged?.Invoke(e.OutputState);
    }
    
    private void ObsStreamStatusChanged(object sender, StreamStatusEventArgs status)
    {
        StreamStatus = status;
        StreamStatusChanged?.Invoke(status);
        Plugin.Log.Info($"Stream Status Changed " +
                         $"(T:{status.TotalStreamTime}s) " +
                         $"(B:{status.KbitsPerSec / 1024f:N2}Mbps) " +
                         $"(F:{status.TotalFrames}) " +
                         $"(D:{status.DroppedFrames})");
    }

    private void ObsHeartBeat(object sender, HeartBeatEventArgs e)
    {
        HeartBeat = e;
        HeartBeatChanged?.Invoke(e);
    }

    private async Task TryFetchSceneList()
    {
        try
        {
            var sceneList = await Obs.GetSceneList();
            var currentScene = await Obs.GetCurrentScene();
            
            sceneNames.Clear();
            sceneNames.AddRange(sceneList.Scenes.Select(s => s.Name));
            SceneNamesUpdated?.Invoke(sceneNames);
            
            CurrentScene = currentScene.Name;
            SceneChanged?.Invoke(CurrentScene);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error getting scene list: {ex.Message}");
            Plugin.Log.Debug(ex);
        }
    }
}