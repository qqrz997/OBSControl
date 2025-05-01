using System;
using System.Collections.Generic;
using BeatSaberMarkupLanguage.Attributes;
using JetBrains.Annotations;
using OBSControl.Managers;
using Zenject;

namespace OBSControl.UI;

internal class PluginSettingsView : IInitializable, IDisposable
{
    private readonly ObsManager obsManager;
    private readonly PluginConfig pluginConfig;
    
    public PluginSettingsView(
        ObsManager obsManager,
        PluginConfig pluginConfig,
        AudioDeviceSettingsView audioDeviceSettingsView,
        SceneSettingsView sceneSettingsView,
        RecordingSettingsView recordingSettingsView)
    {
        this.obsManager = obsManager;
        this.pluginConfig = pluginConfig;

        AudioDeviceSettingsView = audioDeviceSettingsView;
        SceneSettingsView = sceneSettingsView;
        RecordingSettingsView = recordingSettingsView;
    }

    [UsedImplicitly]
    public AudioDeviceSettingsView AudioDeviceSettingsView { get; }
    
    [UsedImplicitly]
    public SceneSettingsView SceneSettingsView { get; }
    
    [UsedImplicitly]
    public RecordingSettingsView RecordingSettingsView { get; }

    public void Initialize()
    {
        obsManager.SceneNamesUpdated += UpdateSceneOptions;
    }

    public void Dispose()
    {
        obsManager.SceneNamesUpdated -= UpdateSceneOptions;
    }

    [UIAction("#post-parse")]
    public void PostParse()
    {
        UpdateSceneOptions(obsManager.SceneNames);
    }

    public bool Enabled
    {
        get => pluginConfig.Enabled;
        set => pluginConfig.Enabled = value;
    }

    public string WsIpAddress
    {
        get => pluginConfig.WsIpAddress ?? string.Empty;
        set => pluginConfig.WsIpAddress = value;
    }

    public string WsPort
    {
        get => pluginConfig.WsPort ?? string.Empty;
        set => pluginConfig.WsPort = value;
    }
    
    public string ServerPassword
    {
        get => pluginConfig.ServerPassword ?? string.Empty;
        set => pluginConfig.ServerPassword = value;
    }

    public string FloatToSeconds(float val) => $"{Math.Round(val, 1)}s";
    
    private void UpdateSceneOptions(IEnumerable<string> sceneNames)
    {
        SceneSettingsView.UpdateSceneOptions(sceneNames);
    }
}