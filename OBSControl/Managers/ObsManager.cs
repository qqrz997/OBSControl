using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IPA.Utilities.Async;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using Zenject;

namespace OBSControl.Managers;

internal class ObsManager : IInitializable, IDisposable
{
    private readonly PluginConfig pluginConfig;

    public ObsManager(PluginConfig pluginConfig)
    {
        this.pluginConfig = pluginConfig;

        Obs = CreateObsInstance();
    }

    private bool onConnectTriggered;

    public OBSWebsocket Obs { get; }
    
    public async void Initialize()
    {
        try
        {
            await RepeatTryConnect();
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e);
        }
    }

    public void Dispose()
    {
        if (Obs.IsConnected)
        {
            Obs.Disconnect();
        }
        Obs.Connected -= OnConnect;
        Obs.RecordingStateChanged -= OnRecordingStateChanged;
        Obs.StreamingStateChanged -= Obs_StreamingStateChanged;
        Obs.StreamStatus -= Obs_StreamStatus;
        Obs.SceneListChanged -= OnObsSceneListChanged;
    }

    private OBSWebsocket CreateObsInstance()
    {
        var newObs = new OBSWebsocket();
        newObs.Connected += OnConnect;
        newObs.StreamingStateChanged += Obs_StreamingStateChanged;
        newObs.StreamStatus += Obs_StreamStatus;
        newObs.SceneListChanged += OnObsSceneListChanged;
        return newObs;
    }

    private readonly HashSet<EventHandler<OutputState>> recordingStateChangedHandlers = [];
    public event EventHandler<OutputState> RecordingStateChanged
    {
        add
        {
            recordingStateChangedHandlers.Add(value);
            if (recordingStateChangedHandlers.Count == 1) Obs.RecordingStateChanged += OnRecordingStateChanged;
        }
        remove
        {
            recordingStateChangedHandlers.Remove(value);
            if (recordingStateChangedHandlers.Count == 0) Obs.RecordingStateChanged -= OnRecordingStateChanged;
        }
    }

    private void OnRecordingStateChanged(object sender, OutputStateChangedEventArgs outputState)
    {
        foreach (var handler in recordingStateChangedHandlers) handler.Invoke(this, outputState.OutputState);
    }

    private string? lastTryConnectMessage;
    private async Task<bool> TryConnect()
    {
        var serverAddress = pluginConfig.ServerAddress;
        if(string.IsNullOrEmpty(serverAddress))
        {
            Plugin.Log.Error("ServerAddress cannot be null or empty.");
            return false;
        }
        if (Obs is { IsConnected: false })
        {
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
        }
        else
            Plugin.Log.Info("TryConnect: OBS is already connected.");
        return Obs.IsConnected;
    }

    private async Task RepeatTryConnect()
    {
        try
        {
            if (string.IsNullOrEmpty(pluginConfig.ServerAddress))
            {
                Plugin.Log.Error("The ServerAddress in the config is null or empty. Unable to connect to OBS.");
                return;
            }
            Plugin.Log.Info($"Attempting to connect to {pluginConfig.ServerAddress}");
            while (!await TryConnect().ConfigureAwait(false))
            {
                await Task.Delay(5000).ConfigureAwait(false);
            }

            Plugin.Log.Info($"OBS {(await Obs.GetVersion().ConfigureAwait(false)).OBSStudioVersion} is connected.");
            Plugin.Log.Info($"OnConnectTriggered: {onConnectTriggered}");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error in RepeatTryConnect: {ex.Message}");
            Plugin.Log.Debug(ex);
        }
    }

    private async void OnConnect(object sender, EventArgs e)
    {
        onConnectTriggered = true;
        Plugin.Log.Info("OnConnect: Connected to OBS.");
        try
        {
            string[] availableScenes = (await Obs.GetSceneList().ConfigureAwait(false)).Scenes.Select(s => s.Name).ToArray();
            await UnityMainThreadTaskScheduler.Factory.StartNew(() =>
            {
                pluginConfig.UpdateSceneOptions(availableScenes);
            });
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error getting scene list: {ex.Message}");
            Plugin.Log.Debug(ex);
        }
    }

    private async void OnObsSceneListChanged(object sender, EventArgs e)
    {
        try
        {
            string[] availableScenes = (await Obs.GetSceneList().ConfigureAwait(false)).Scenes.Select(s => s.Name).ToArray();
            Plugin.Log.Info($"OBS scene list changed: {string.Join(", ", availableScenes)}");
            await UnityMainThreadTaskScheduler.Factory.StartNew(() =>
            {
                pluginConfig.UpdateSceneOptions(availableScenes);
            });
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error getting scene list: {ex.Message}");
            Plugin.Log.Debug(ex);
        }
    }

    private static void Obs_StreamingStateChanged(object sender, OutputStateChangedEventArgs e)
    {
        Plugin.Log.Info($"Streaming State Changed: {e.OutputState.ToString()}");
    }
    
    private static void Obs_StreamStatus(object sender, StreamStatusEventArgs status)
    {
        Plugin.Log.Info($"Stream Time: {status.TotalStreamTime.ToString()} sec");
        Plugin.Log.Info($"Bitrate: {status.KbitsPerSec / 1024f:N2} Mbps");
        Plugin.Log.Info($"FPS: {status.FPS} FPS");
        Plugin.Log.Info($"Strain: {status.Strain * 100} %");
        Plugin.Log.Info($"DroppedFrames: {status.DroppedFrames.ToString()} frames");
        Plugin.Log.Info($"TotalFrames: {status.TotalFrames.ToString()} frames");
    }
}