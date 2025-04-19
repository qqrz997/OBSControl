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
            recordingFolderPath = await obsManager.Obs.GetRecordingFolder().ConfigureAwait(false);
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
                await obsManager.Obs.SetFilenameFormatting(fileFormat).ConfigureAwait(false);
                currentFormat = await obsManager.Obs.GetFilenameFormatting().ConfigureAwait(false);
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
        string[] availableScenes = await GetAvailableScenes().ConfigureAwait(false);
        if (!availableScenes.Contains(startScene))
            startScene = string.Empty;
        if (!availableScenes.Contains(gameScene))
            gameScene = string.Empty;
        bool validIntro = ValidateScenes(availableScenes, startScene, gameScene);
        try
        {
            if (validIntro)
            {
                int transitionDuration = await obsManager.Obs.GetTransitionDuration().ConfigureAwait(false);
                await obsManager.Obs.SetTransitionDuration(0).ConfigureAwait(false);
                Plugin.Log.Info($"Setting intro OBS scene to '{startScene}'");
                await obsManager.Obs.SetCurrentScene(startScene).ConfigureAwait(false);
                await obsManager.Obs.SetTransitionDuration(transitionDuration).ConfigureAwait(false);
                await obsManager.Obs.StartRecording().ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromSeconds(pluginConfig.StartSceneDuration)).ConfigureAwait(false);
                Plugin.Log.Info($"Setting game OBS scene to '{gameScene}'");
                await obsManager.Obs.SetCurrentScene(gameScene).ConfigureAwait(false);
            }
            else
            {
                if (!string.IsNullOrEmpty(gameScene))
                    await obsManager.Obs.SetCurrentScene(gameScene).ConfigureAwait(false);
                await obsManager.Obs.StartRecording().ConfigureAwait(false);
            }

        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error starting recording in OBS: {ex.Message}");
            Plugin.Log.Debug(ex);
        }
    }

    private async Task<string[]> GetAvailableScenes()
    {
        try
        {
            return (await obsManager.Obs.GetSceneList().ConfigureAwait(false)).Scenes.Select(s => s.Name).ToArray();
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
    
    private async Task TryStopRecordingAsync(string? renameTo)
    {
        string endScene = pluginConfig.EndSceneName;
        string gameScene = pluginConfig.GameSceneName;
        string[] availableScenes = await GetAvailableScenes().ConfigureAwait(false);
        if (!availableScenes.Contains(endScene))
            endScene = string.Empty;
        if (!availableScenes.Contains(gameScene))
            gameScene = string.Empty;
        bool validOutro = ValidateScenes(availableScenes, endScene, gameScene);
        try
        {
            videoRenameString = renameTo;
            float delay = pluginConfig.RecordingStopDelay;
            await obsManager.Obs.StopRecording().ConfigureAwait(false);
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
            case OutputState.Starting:
                recordingCurrentLevel = true;
                break;
            case OutputState.Started:
                recordingCurrentLevel = true;
                Task.Run(() => obsManager.Obs.SetFilenameFormatting(DefaultFileFormat));
                break;
            case OutputState.Stopping:
                recordingCurrentLevel = false;
                break;
            case OutputState.Stopped:
                recordingCurrentLevel = false;
                RenameLastRecording(videoRenameString);
                videoRenameString = null;
                break;
            case OutputState.Unknown:
            case OutputState.Paused:
            case OutputState.Resumed:
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