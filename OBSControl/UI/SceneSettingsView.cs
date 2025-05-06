using System;
using System.Collections.Generic;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using JetBrains.Annotations;
using OBSControl.Utilities;

namespace OBSControl.UI;

internal class SceneSettingsView
{
    private readonly PluginConfig pluginConfig;

    public SceneSettingsView(PluginConfig pluginConfig)
    {
        this.pluginConfig = pluginConfig;
    }
    
    [UIComponent("StartSceneDropdown")] private readonly DropDownListSetting startSceneDropDown = null!;
    [UIComponent("GameSceneDropdown")] private readonly DropDownListSetting gameSceneDropdown = null!;
    [UIComponent("EndSceneDropdown")] private readonly DropDownListSetting endSceneDropdown = null!;
    [UIComponent("PostRecordSceneDropdown")] private readonly DropDownListSetting postRecordSceneDropdown = null!;

    [UIValue("SceneSelectOptions"), UsedImplicitly]
    public readonly List<object> SceneSelectOptions = [string.Empty];
    
    public float StartSceneDuration
    {
        get => pluginConfig.StartSceneDuration;
        set => pluginConfig.StartSceneDuration = value;
    }

    public float EndSceneDuration
    {
        get => pluginConfig.EndSceneDuration;
        set => pluginConfig.EndSceneDuration = value;
    }

    public string StartSceneName
    {
        get => pluginConfig.StartSceneName;
        set => pluginConfig.StartSceneName = value;
    }

    public string GameSceneName
    {
        get => pluginConfig.GameSceneName;
        set => pluginConfig.GameSceneName = value;
    }

    public string EndSceneName
    {
        get => pluginConfig.EndSceneName;
        set => pluginConfig.EndSceneName = value;
    }

    public float EndSceneDelay
    {
        get => pluginConfig.EndSceneDelay;
        set => pluginConfig.EndSceneDelay = value;
    }

    public string PostRecordSceneName
    {
        get => pluginConfig.PostRecordSceneName;
        set => pluginConfig.PostRecordSceneName = value;
    }

    public string FloatToSeconds(float val) => $"{Math.Round(val, 1)}s";
    
    public void UpdateSceneOptions(IEnumerable<string> newOptions)
    {
        SceneSelectOptions.Clear();
        SceneSelectOptions.Add(string.Empty);
        SceneSelectOptions.AddRange(newOptions.Distinct());
        
        startSceneDropDown.RefreshDropdown();
        gameSceneDropdown.RefreshDropdown();
        endSceneDropdown.RefreshDropdown();
        postRecordSceneDropdown.RefreshDropdown();
    }
}