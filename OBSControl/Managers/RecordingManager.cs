using System;
using System.IO;
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
    private string? newRecordingFilename;
    private string? lastRecordingFilename;

    public void Initialize()
    {
        eventManager.RecordingStateChanged += RecordingStateChanged;
    }

    public void Dispose()
    {
        eventManager.RecordingStateChanged -= RecordingStateChanged;
        if (!recordingCurrentLevel) return;
        lastRecordingFilename = obsWebsocket.StopRecord();
        recordingCurrentLevel = false;
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

            if (recordingCurrentLevel)
            {
                Task.Run(StopRecording);
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Encountered an error during end of level: {ex}");
        }
    }

    private async Task StopRecording()
    {
        if (!obsWebsocket.IsConnected) return;
        
        try
        {
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

    private void StartRecording()
    {
        if (obsWebsocket.IsConnected && !obsWebsocket.GetRecordStatus().IsRecording) obsWebsocket.StartRecord();
    }

    private void RecordingStateChanged(OutputState type)
    {
        Plugin.Log.Debug($"Recording State Changed: {type}");
        switch (type)
        {
            case OutputState.OBS_WEBSOCKET_OUTPUT_STARTING or OutputState.OBS_WEBSOCKET_OUTPUT_STARTED:
                recordingCurrentLevel = true;
                break;
            case OutputState.OBS_WEBSOCKET_OUTPUT_STOPPING:
                recordingCurrentLevel = false;
                break;
            case OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED:
                recordingCurrentLevel = false;
                HandleRecordingStopped();
                break;
            case OutputState.OBS_WEBSOCKET_OUTPUT_PAUSED:
            case OutputState.OBS_WEBSOCKET_OUTPUT_RESUMED:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    private void HandleRecordingStopped()
    {
        if (newRecordingFilename is not null)
        {
            RenameLastRecording(newRecordingFilename);
            newRecordingFilename = null;
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
}