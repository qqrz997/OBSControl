using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OBSControl.Models;
using OBSControl.Utilities;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using Zenject;

namespace OBSControl.Managers;

internal class RecordingManager : IInitializable, IDisposable
{
    private readonly PluginConfig pluginConfig;
    private readonly EventManager eventManager;
    private readonly SceneManager sceneManager;
    private readonly IOBSWebsocket obsWebsocket;

    public RecordingManager(
        PluginConfig pluginConfig,
        EventManager eventManager,
        SceneManager sceneManager,
        IOBSWebsocket obsWebsocket)
    {
        this.pluginConfig = pluginConfig;
        this.eventManager = eventManager;
        this.sceneManager = sceneManager;
        this.obsWebsocket = obsWebsocket;
    }
    
    private bool recordingCurrentLevel;
    private bool restartRecording;
    private string? newRecordingFilename;
    private string? lastRecordingFilename;
    private CancellationTokenSource restartRecordTokenSource = new();
    private CancellationTokenSource recordStatusTokenSource = new();
    
    public event Action<RecordStatusChangedEventArgs>? RecordStatusChanged;
    
    public RecordStatusChangedEventArgs? CurrentRecordingStatus { get; private set; }

    public void Initialize()
    {
        eventManager.RecordingStateChanged += RecordingStateChanged;
    }

    public void Dispose()
    {
        eventManager.RecordingStateChanged -= RecordingStateChanged;
        restartRecordTokenSource.Cancel();
        restartRecordTokenSource.Dispose();
        if (!recordingCurrentLevel) return;
        lastRecordingFilename = obsWebsocket.StopRecord();
        recordingCurrentLevel = false;
        StopPollingRecordStatus();
    }
    
    public void ManualToggleRecording()
    {
        if (!obsWebsocket.IsConnected) return;
        if (!obsWebsocket.GetRecordStatus().IsRecording)
        {
            obsWebsocket.StartRecord();
            recordingCurrentLevel = true;
        }
        else
        {
            lastRecordingFilename = obsWebsocket.StopRecord();
            recordingCurrentLevel = false;
        }
    }
    
    public async Task StartRecordingLevel(Action initialTransitionCallback)
    {
        if (!obsWebsocket.IsConnected) return;
        
        try
        {
            if (!pluginConfig.UseSceneTransitions)
            {
                // Wait for the initial scene to be shown before starting recording
                await sceneManager.TransitionToScene(pluginConfig.GameSceneName);
                initialTransitionCallback();
                StartRecording();
                return;
            }

            // Try transition to start scene
            if (await sceneManager.TransitionToScene(pluginConfig.StartSceneName))
            {
                StartRecording();
                await Task.Delay(TimeSpan.FromSeconds(pluginConfig.StartSceneDuration));
                initialTransitionCallback();
            }
            else
            {
                StartRecording();
                initialTransitionCallback();
            }

            await sceneManager.TransitionToScene(pluginConfig.GameSceneName);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Encountered an error while trying to start recording: {ex}");
        }
    }

    public void OnStandardLevelFinished(ExtendedLevelData levelData, ExtendedCompletionResults levelCompletionResults)
    {
        try
        {
            newRecordingFilename = FileRenaming.GetFilenameString(
                pluginConfig.RecordingFileFormat, levelData, levelCompletionResults, 
                pluginConfig.InvalidCharacterSubstitute, pluginConfig.ReplaceSpacesWith);

            if (!recordingCurrentLevel) return;

            var restartAction = pluginConfig.RestartAction;

            if (levelCompletionResults.LevelEndAction is LevelCompletionResults.LevelEndAction.Restart
                && restartAction is RestartAction.ContinueRecording)
            {
                // Keep recording and do nothing
                return;
            }

            Task.Run(() => StopRecording(restartAction is RestartAction.RestartRecording));
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Encountered an error during end of level: {ex}");
        }
    }

    private async Task StopRecording(bool restart)
    {
        if (!obsWebsocket.IsConnected) return;
        try
        {
            if (restart)
            {
                lastRecordingFilename = obsWebsocket.StopRecord();
                restartRecording = true;
                return;
            }
            
            await Task.Delay(TimeSpan.FromSeconds(pluginConfig.RecordingStopDelay));
            if (!pluginConfig.UseSceneTransitions)
            {
                lastRecordingFilename = obsWebsocket.StopRecord();
                recordingCurrentLevel = false;
                return;
            }

            // Transition from game scene to end scene
            await Task.Delay(TimeSpan.FromSeconds(pluginConfig.EndSceneDelay));
            if (await sceneManager.TransitionToScene(pluginConfig.EndSceneName))
            {
                await Task.Delay(TimeSpan.FromSeconds(pluginConfig.EndSceneDuration));
            }

            lastRecordingFilename = obsWebsocket.StopRecord();
            recordingCurrentLevel = false;

            await sceneManager.TransitionToScene(pluginConfig.PostRecordSceneName);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Encountered an error while trying to stop recording: {ex}");
        }
    }

    private async Task StartRecordingWhenFinished(int timeout, CancellationToken token)
    {
        while (obsWebsocket.IsConnected && obsWebsocket.GetRecordStatus().IsRecording)
        {
            if (timeout <= 0 || token.IsCancellationRequested) return;
            timeout -= 250;
            await Task.Delay(250, token);
        }
        StartRecording();
    }
    
    private void StartRecording()
    {
        Plugin.Log.Debug("Attempting to start recording...");
        if (obsWebsocket.IsConnected && !obsWebsocket.GetRecordStatus().IsRecording) obsWebsocket.StartRecord();
    }

    private void RecordingStateChanged(OutputState type)
    {
        Plugin.Log.Debug($"Recording State Changed: {type}");
        switch (type)
        {
            case OutputState.OBS_WEBSOCKET_OUTPUT_STARTING:
                recordingCurrentLevel = true;
                break;
            case OutputState.OBS_WEBSOCKET_OUTPUT_STARTED:
                recordingCurrentLevel = true;
                StartPollingRecordStatus();
                break;
            case OutputState.OBS_WEBSOCKET_OUTPUT_STOPPING:
                recordingCurrentLevel = false;
                break;
            case OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED:
                recordingCurrentLevel = false;
                StopPollingRecordStatus();
                if (newRecordingFilename is not null)
                {
                    RenameLastRecording(newRecordingFilename);
                    newRecordingFilename = null;
                }
                if (restartRecording)
                {
                    restartRecording = false;
                    restartRecordTokenSource.Cancel();
                    restartRecordTokenSource.Dispose();
                    restartRecordTokenSource = new();
                    // Restart has to be delayed because the websocket doesn't update status before state change
                    Task.Run(() => StartRecordingWhenFinished(4000, restartRecordTokenSource.Token));
                }
                break;
            case OutputState.OBS_WEBSOCKET_OUTPUT_PAUSED:
            case OutputState.OBS_WEBSOCKET_OUTPUT_RESUMED:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    private void RenameLastRecording(string newNameWithoutExtension)
    {
        if (!obsWebsocket.IsConnected)
        {
            return;
        }
        
        try
        {
            if (string.IsNullOrEmpty(newNameWithoutExtension))
            {
                Plugin.Log.Warn("Skipping file rename, new name is invalid.");
                return;
            }

            if (string.IsNullOrEmpty(lastRecordingFilename))
            {
                Plugin.Log.Warn("Couldn't determine last recording filename, unable to rename.");
                return;
            }
        
            var lastRecordingFile = new FileInfo(lastRecordingFilename!);
            if (!lastRecordingFile.Exists)
            {
                Plugin.Log.Warn($"Couldn't find last recording file '{lastRecordingFilename}', unable to rename.");
                return;
            }
        
            var recordDirectory = obsWebsocket.GetRecordDirectory();
            if (string.IsNullOrEmpty(recordDirectory))
            {
                Plugin.Log.Warn("Unable to determine current recording folder, unable to rename.");
                return;
            }
        
            var directory = new DirectoryInfo(recordDirectory);
            if (!directory.Exists)
            {
                Plugin.Log.Warn("Recording directory doesn't exist, unable to rename.");
                return;
            }
            
            string newFileName = $"{newNameWithoutExtension}{lastRecordingFile.Extension}";
            Plugin.Log.Info($"Attempting to rename {lastRecordingFile.Name} to {newFileName}");
            lastRecordingFile.MoveTo(PathExt.UniqueCombine(directory.FullName, newFileName));
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Encountered an error while trying to rename last recording: {ex}");
        }
    }

    private void StartPollingRecordStatus()
    {
        recordStatusTokenSource = new();
        Task.Run(() => RepeatPollRecordStatus(recordStatusTokenSource.Token));
    }

    private void StopPollingRecordStatus()
    {
        recordStatusTokenSource.Cancel();
        recordStatusTokenSource.Dispose();
    }

    private long lastRecordingBytes;
    
    private async Task RepeatPollRecordStatus(CancellationToken token)
    {
        const int interval = 2500;

        try
        {
            while (!token.IsCancellationRequested && obsWebsocket.IsConnected)
            {
                var status = obsWebsocket.GetRecordStatus();
                
                var bitrate = (status.RecordingBytes - lastRecordingBytes) / (interval / 1000) * 8;
                lastRecordingBytes = status.RecordingBytes;

                CurrentRecordingStatus = new(status, bitrate);
                RecordStatusChanged?.Invoke(CurrentRecordingStatus);
                await Task.Delay(interval, token);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Encountered a problem while polling record status: {ex}");
        }
    }
}