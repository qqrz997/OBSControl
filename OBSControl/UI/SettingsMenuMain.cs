﻿using System;
using System.Collections.Generic;
using BeatSaberMarkupLanguage.Attributes;
using JetBrains.Annotations;
using OBSControl.Managers;
using Zenject;

namespace OBSControl.UI;

internal class SettingsMenuMain : IInitializable, IDisposable
{
    private readonly PluginConfig pluginConfig;
    private readonly EventManager eventManager;
    private readonly SceneManager sceneManager;

    public SettingsMenuMain(
        PluginConfig pluginConfig,
        EventManager eventManager,
        SceneManager sceneManager,
        SettingsMenuScene settingsMenuScene,
        SettingsMenuRecording settingsMenuRecording)
    {
        this.pluginConfig = pluginConfig;
        this.eventManager = eventManager;
        this.sceneManager = sceneManager;

        SettingsMenuScene = settingsMenuScene;
        SettingsMenuRecording = settingsMenuRecording;
    }

    [UsedImplicitly]
    public SettingsMenuScene SettingsMenuScene { get; }
    
    [UsedImplicitly]
    public SettingsMenuRecording SettingsMenuRecording { get; }

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
        SettingsMenuScene.UpdateSceneOptions(sceneNames);
    }
}