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
    private const string DefaultFileFormat = "%CCYY-%MM-%DD %hh-%mm-%ss";
    private const string DefaultDateTimeFormat = "yyyyMMddHHmmss";

    private readonly PluginConfig pluginConfig;
    private readonly ObsManager obsManager;
    private readonly PlayerDataModel playerDataModel;

    public RecordingManager(
        PluginConfig pluginConfig,
        ObsManager obsManager,
        PlayerDataModel playerDataModel)
    {
        this.pluginConfig = pluginConfig;
        this.obsManager = obsManager;
        this.playerDataModel = playerDataModel;
    }
    
    private bool recordingCurrentLevel;

    private string? recordingFolderPath;
    private string? currentFileFormat;
    private string? videoRenameString;

    public void Initialize()
    {
        obsManager.RecordingStateChanged += ObsRecordingStateChanged;
    }

    public void Dispose()
    {
        obsManager.RecordingStateChanged -= ObsRecordingStateChanged;
        StopIfRecording(string.Empty);
    }

    private async Task TryStartRecordingAsync(string fileFormat)
    {
        Plugin.Log.Debug("TryStartRecording");
        try
        {
            recordingFolderPath = obsManager.Obs.GetRecordDirectory();
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error getting recording folder from OBS: {ex.Message}");
            Plugin.Log.Debug(ex);
            return;
        }

        int tries = 0;
        string? currentFormat = null;
        do
        {
            if (tries > 0)
            {
                Plugin.Log.Debug($"({tries}) Failed to set OBS's FilenameFormatting to {fileFormat} retrying in 50ms");
                await Task.Delay(50);
            }
            tries++;
            try
            {
                // await obsManager.Obs.SetFilenameFormatting(fileFormat);
                // currentFormat = await obsManager.Obs.GetFilenameFormatting();
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Error getting current filename format from OBS: {ex.Message}");
                Plugin.Log.Debug(ex);
            }
        } while (currentFormat != fileFormat && tries < 10);
        
        currentFileFormat = fileFormat;
        string startScene = pluginConfig.StartSceneName;
        string gameScene = pluginConfig.GameSceneName;
        var availableScenes = GetAvailableScenes();
        if (!availableScenes.Contains(startScene))
            startScene = string.Empty;
        if (!availableScenes.Contains(gameScene))
            gameScene = string.Empty;
        bool validIntro = ValidateScenes(availableScenes, startScene, gameScene);
        try
        {
            if (validIntro)
            {
                var transitionDuration = obsManager.Obs.GetCurrentSceneTransition().Duration.GetValueOrDefault();
                obsManager.Obs.SetCurrentSceneTransitionDuration(0);
                Plugin.Log.Info($"Setting intro OBS scene to '{startScene}'");
                obsManager.Obs.SetCurrentProgramScene(startScene);
                obsManager.Obs.SetCurrentSceneTransitionDuration(transitionDuration);
                obsManager.Obs.StartRecord();
                await Task.Delay(TimeSpan.FromSeconds(pluginConfig.StartSceneDuration));
                Plugin.Log.Info($"Setting game OBS scene to '{gameScene}'");
                obsManager.Obs.SetCurrentProgramScene(gameScene);
            }
            else
            {
                if (!string.IsNullOrEmpty(gameScene))
                    obsManager.Obs.SetCurrentProgramScene(gameScene);
                obsManager.Obs.StartRecord();
            }

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

    private void StopIfRecording(string fileName)
    {
        if (recordingCurrentLevel)
        {
            Task.Run(() => TryStopRecordingAsync(fileName));
        }
    }
    
    private void TryStopRecordingAsync(string? renameTo)
    {
        string endScene = pluginConfig.EndSceneName;
        string gameScene = pluginConfig.GameSceneName;
        string[] availableScenes = GetAvailableScenes();
        if (!availableScenes.Contains(endScene))
            endScene = string.Empty;
        if (!availableScenes.Contains(gameScene))
            gameScene = string.Empty;
        bool validOutro = ValidateScenes(availableScenes, endScene, gameScene);
        try
        {
            videoRenameString = renameTo;
            float delay = pluginConfig.RecordingStopDelay;
            obsManager.Obs.StopRecord();
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

    private void RenameLastRecording(string? newName)
    {
        if (newName == null)
        {
            Plugin.Log.Warn("Unable to rename last recording, provided new name is null.");
            return;
        }
        if (newName.Length == 0)
        {
            Plugin.Log.Info("Skipping file rename, no RecordingFileFormat provided.");
            return;
        }
        string? recordingFolder = recordingFolderPath;
        string? fileFormat = currentFileFormat;
        currentFileFormat = null;
        if (string.IsNullOrEmpty(recordingFolder))
        {
            Plugin.Log.Warn("Unable to determine current recording folder, unable to rename.");
            return;
        }
        if (string.IsNullOrEmpty(fileFormat))
        {
            Plugin.Log.Warn("Last recorded filename not stored, unable to rename.");
            return;
        }

        var directory = new DirectoryInfo(recordingFolder);
        if (!directory.Exists)
        {
            Plugin.Log.Warn("Recording directory doesn't exist, unable to rename.");
            return;
        }
        var targetFile = directory.GetFiles(fileFormat + "*").OrderByDescending(f => f.CreationTimeUtc).FirstOrDefault();
        if (targetFile == null)
        {
            Plugin.Log.Warn("Couldn't find recorded file, unable to rename.");
            return;
        }

        string fileExtension = targetFile.Extension;
        Plugin.Log.Info($"Attempting to rename {fileFormat}{fileExtension} to {newName} with an extension of {fileExtension}");
        string newFile = newName + fileExtension;
        int index = 2;
        while (File.Exists(Path.Combine(directory.FullName, newFile)))
        {
            Plugin.Log.Debug($"File exists: {Path.Combine(directory.FullName, newFile)}");
            newFile = newName + $"({index})" + fileExtension;
            index++;
        }
        try
        {
            Plugin.Log.Debug($"Attempting to rename to '{newFile}'");
            targetFile.MoveTo(Path.Combine(directory.FullName, newFile));
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Unable to rename {targetFile.Name} to {newFile}: {ex.Message}");
            Plugin.Log.Debug(ex);
        }
    }

    public void StartRecordingLevel()
    {
        string fileFormat = DateTime.Now.ToString(DefaultDateTimeFormat);
        Plugin.Log.Debug($"Starting recording, file format: {fileFormat}");
        Task.Run(() => TryStartRecordingAsync(fileFormat));
    }

    public void OnStandardLevelFinished(ExtendedLevelData levelData, ExtendedCompletionResults levelCompletionResults)
    {
        try
        {
            var newFileName = FileRenaming.GetFilenameString(
                pluginConfig.RecordingFileFormat, levelData, levelCompletionResults, 
                pluginConfig.InvalidCharacterSubstitute, pluginConfig.ReplaceSpacesWith);

            StopIfRecording(newFileName);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error generating new file name: {ex}");
            Plugin.Log.Debug(ex);
        }
    }

    private void ObsRecordingStateChanged(OutputState type)
    {
        Plugin.Log.Debug($"Recording State Changed: {type}");
        switch (type)
        {
            case OutputState.OBS_WEBSOCKET_OUTPUT_STARTING:
            case OutputState.OBS_WEBSOCKET_OUTPUT_STARTED:
                recordingCurrentLevel = true;
                break;
            case OutputState.OBS_WEBSOCKET_OUTPUT_STOPPING:
                recordingCurrentLevel = false;
                break;
            case OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED:
                recordingCurrentLevel = false;
                RenameLastRecording(videoRenameString);
                videoRenameString = null;
                break;
            case OutputState.OBS_WEBSOCKET_OUTPUT_PAUSED:
            case OutputState.OBS_WEBSOCKET_OUTPUT_RESUMED:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    private static bool ValidateScenes(IEnumerable<string> availableScenes, params string[] scenes)
    {
        return scenes.Length != 0 && scenes.All(s => !string.IsNullOrEmpty(s) && availableScenes.Contains(s));
    }
}