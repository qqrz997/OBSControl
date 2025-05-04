using System;
using System.Collections.Generic;
using BeatSaberMarkupLanguage.Attributes;
using JetBrains.Annotations;
using OBSControl.Managers;
using Zenject;

namespace OBSControl.UI;

internal class PluginSettingsView : IInitializable, IDisposable
{
    private readonly PluginConfig pluginConfig;
    private readonly EventManager eventManager;
    private readonly SceneManager sceneManager;

    public PluginSettingsView(
        PluginConfig pluginConfig,
        EventManager eventManager,
        SceneManager sceneManager,
        AudioDeviceSettingsView audioDeviceSettingsView,
        SceneSettingsView sceneSettingsView,
        RecordingSettingsView recordingSettingsView)
    {
        this.pluginConfig = pluginConfig;
        this.eventManager = eventManager;
        this.sceneManager = sceneManager;

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
        eventManager.SceneNamesUpdated += UpdateSceneOptions;
    }

    public void Dispose()
    {
        eventManager.SceneNamesUpdated -= UpdateSceneOptions;
    }

    [UIAction("#post-parse")]
    public void PostParse()
    {
        UpdateSceneOptions(sceneManager.SceneNames);
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