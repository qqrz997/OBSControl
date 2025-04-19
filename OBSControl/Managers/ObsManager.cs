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

    private bool isConnected = false;

    public event Action<bool>? ConnectionStateChanged; 
    public event Action<IEnumerable<string>>? SceneNamesUpdated; 
    public event Action<OutputState>? RecordingStateChanged;
    public event Action<OutputState>? StreamingStateChanged;
    public event Action<StreamStatusEventArgs>? StreamStatusChanged;
    public event Action<HeartBeatEventArgs>? HeartBeat;
    
    public OBSWebsocket Obs { get; }
    public IEnumerable<string> SceneNames => sceneNames;
    
    public void Initialize()
    {
        Obs.Connected += ObsConnected;
        Obs.Disconnected += ObsDisconnected;
        Obs.RecordingStateChanged += ObsRecordingStateChanged;
        Obs.StreamingStateChanged += ObsStreamingStateChanged;
        Obs.StreamStatus += ObsStreamStatusChanged;
        Obs.SceneListChanged += ObsSceneListChanged;
        Obs.Heartbeat += ObsHeartBeat;
        
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
        if (isConnected)
        {
            return;
        }
        
        Plugin.Log.Info("OBS Connected.");
        ConnectionStateChanged?.Invoke(isConnected = true);
        await TryFetchSceneList();
    }
    
    private void ObsDisconnected(object sender, EventArgs e)
    {
        if (!isConnected)
        {
            return;
        }
        
        Plugin.Log.Info("OBS Disconnected.");
        ConnectionStateChanged?.Invoke(isConnected = false);
    }

    private async void ObsSceneListChanged(object sender, EventArgs e)
    {
        await TryFetchSceneList();
    }

    private void ObsRecordingStateChanged(object sender, OutputStateChangedEventArgs e)
    {
        Plugin.Log.Info($"Recording State Changed: {e.OutputState}");
        RecordingStateChanged?.Invoke(e.OutputState);
    }

    private void ObsStreamingStateChanged(object sender, OutputStateChangedEventArgs e)
    {
        Plugin.Log.Info($"Streaming State Changed: {e.OutputState}");
        StreamingStateChanged?.Invoke(e.OutputState);
    }
    
    private void ObsStreamStatusChanged(object sender, StreamStatusEventArgs status)
    {
        StreamStatusChanged?.Invoke(status);
        Plugin.Log.Info($"Stream Status Changed\n" +
                        $"Stream Time: {status.TotalStreamTime}s\n" +
                        $"Bitrate: {status.KbitsPerSec / 1024f:N2} Mbps\n" +
                        $"FPS: {status.FPS} FPS\n" +
                        $"Strain: {status.Strain * 100}%\n" +
                        $"DroppedFrames: {status.DroppedFrames} frames\n" +
                        $"TotalFrames: {status.TotalFrames} frames");
    }

    private void ObsHeartBeat(object sender, HeartBeatEventArgs e)
    {
        HeartBeat?.Invoke(e);
    }

    private async Task TryFetchSceneList()
    {
        try
        {
            var sceneList = await Obs.GetSceneList().ConfigureAwait(false);
            sceneNames.Clear();
            sceneNames.AddRange(sceneList.Scenes.Select(s => s.Name));
            SceneNamesUpdated?.Invoke(sceneNames);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error getting scene list: {ex.Message}");
            Plugin.Log.Debug(ex);
        }
    }
}