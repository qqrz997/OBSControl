using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    private readonly ObsManager obsManager;

    public RecordingManager(
        PluginConfig pluginConfig,
        ObsManager obsManager)
    {
        this.pluginConfig = pluginConfig;
        this.obsManager = obsManager;
    }
    
    private bool recordingCurrentLevel;
    private bool switchToGameSceneOnRecordEnd;
    private string? newRecordingFilename;
    private string? lastRecordingFilename;

    public void Initialize()
    {
        obsManager.RecordingStateChanged += ObsRecordingStateChanged;
    }

    public void Dispose()
    {
        obsManager.RecordingStateChanged -= ObsRecordingStateChanged;
        if (recordingCurrentLevel) StopRecordingImmediately();
    }

    public void StartRecordingLevel()
    {
        Task.Run(TryStartRecordingAsync);
    }

    public void OnStandardLevelFinished(ExtendedLevelData levelData, ExtendedCompletionResults levelCompletionResults)
    {
        try
        {
            var newFileName = FileRenaming.GetFilenameString(
                pluginConfig.RecordingFileFormat, levelData, levelCompletionResults, 
                pluginConfig.InvalidCharacterSubstitute, pluginConfig.ReplaceSpacesWith);

            if (recordingCurrentLevel) Task.Run(() => StopRecording(newFileName));
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error generating new file name: {ex}");
            Plugin.Log.Debug(ex);
        }
    }
    
    private async Task TryStartRecordingAsync()
    {
        if (obsManager.Obs.GetRecordStatus().IsRecording)
        {
            return;
        }
        
        var availableScenes = GetAvailableScenes();
        var startScene = availableScenes.Contains(pluginConfig.StartSceneName) ? pluginConfig.StartSceneName : string.Empty;
        var gameScene = availableScenes.Contains(pluginConfig.GameSceneName) ? pluginConfig.GameSceneName : string.Empty;
        try
        {
            if (!ValidateScenes(availableScenes, startScene, gameScene))
            {
                if (!string.IsNullOrEmpty(gameScene))
                    obsManager.Obs.SetCurrentProgramScene(gameScene);
                obsManager.Obs.StartRecord();
                return;
            }

            Plugin.Log.Info($"Setting intro OBS scene to '{startScene}'");
            obsManager.Obs.SetCurrentProgramScene(startScene);
            obsManager.Obs.StartRecord();
            await Task.Delay(TimeSpan.FromSeconds(pluginConfig.StartSceneDuration));
            Plugin.Log.Info($"Setting game OBS scene to '{gameScene}'");
            obsManager.Obs.SetCurrentProgramScene(gameScene);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error starting recording in OBS: {ex.Message}");
            Plugin.Log.Debug(ex);
        }
    }

    private string[] GetAvailableScenes()
    {
        try
        {
            return obsManager.Obs.GetSceneList().Scenes.Select(s => s.Name).ToArray();
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error validating scenes: {ex.Message}");
            Plugin.Log.Debug(ex);
            return [];
        }
    }
    
    private async Task StopRecording(string fileName)
    {
        try
        { 
            if (pluginConfig.RecordingStopDelay > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(pluginConfig.RecordingStopDelay));
            }
            
            var availableScenes = GetAvailableScenes();
            var endScene = availableScenes.Contains(pluginConfig.EndSceneName) ? pluginConfig.EndSceneName : string.Empty;
            var gameScene = availableScenes.Contains(pluginConfig.GameSceneName) ? pluginConfig.GameSceneName : string.Empty;
            if (ValidateScenes(availableScenes, endScene, gameScene))
            {
                Plugin.Log.Info($"Setting outro OBS scene to '{endScene}' for {pluginConfig.EndSceneDuration:F1}s");
                obsManager.Obs.SetCurrentProgramScene(endScene);
                await Task.Delay(TimeSpan.FromSeconds(pluginConfig.EndSceneDuration));
            }
            
            newRecordingFilename = fileName;
            switchToGameSceneOnRecordEnd = true;
            lastRecordingFilename = obsManager.Obs.StopRecord();
            recordingCurrentLevel = false;
        }
        catch (ErrorResponseException ex)
        {
            Plugin.Log.Error($"Error trying to stop recording: {ex.Message}");
            if (ex.Message != "recording not active")
                Plugin.Log.Debug(ex);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Unexpected exception trying to stop recording: {ex.Message}");
            Plugin.Log.Debug(ex);
        }
    }

    private void StopRecordingImmediately()
    {
        switchToGameSceneOnRecordEnd = true;
        obsManager.Obs.StopRecord();
        recordingCurrentLevel = false;
    }

    private void ObsRecordingStateChanged(OutputState type)
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
        if (switchToGameSceneOnRecordEnd)
        {
            
        }
        
        if (newRecordingFilename is not null)
        {
            RenameLastRecording(newRecordingFilename);
            newRecordingFilename = null;
        }
    }

    private void RenameLastRecording(string newNameWithoutExtension)
    {
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
        
            var recordDirectory = obsManager.Obs.GetRecordDirectory();
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
            Plugin.Log.Error($"Unable to rename last recording: {ex.Message}");
            Plugin.Log.Debug(ex);
        }
    }

    private static bool ValidateScenes(IEnumerable<string> availableScenes, params string[] scenes)
    {
        return scenes.Length != 0 && scenes.All(s => !string.IsNullOrEmpty(s) && availableScenes.Contains(s));
    }
}