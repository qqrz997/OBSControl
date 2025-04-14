using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using IPA.Config.Stores;
using IPA.Config.Stores.Attributes;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace OBSControl;

internal class PluginConfig
{
    private float levelStartDelay = 3f;
    private float recordingStopDelay = 4f;
    private float startSceneDuration = 1f;
    private float endSceneDuration = 2f;
    
    public virtual bool Enabled { get; set; } = true;
    
    public virtual string? ServerAddress { get; set; } = "ws://127.0.0.1:4444";
    
    public virtual string? ServerPassword { get; set; } = string.Empty;
    
    public virtual float LevelStartDelay
    {
        get => levelStartDelay;
        set
        {
            if (value < 0) value = 0;
            levelStartDelay = (float)Math.Round(value, 1);
        }
    }
    
    public virtual float RecordingStopDelay
    {
        get => recordingStopDelay;
        set
        {
            if (value < 0) value = 0;
            recordingStopDelay = (float)Math.Round(value, 1);
        }
    }
    
    public virtual string? RecordingFileFormat { get; set; } = "?N{20}-?A{20}_?%<_[?M]><-?F><-?e>";
    
    public virtual string? ReplaceSpacesWith { get; set; } = "_";
    
    public virtual string? InvalidCharacterSubstitute { get; set; } = "_";

    public virtual float StartSceneDuration
    {
        get => startSceneDuration;
        set
        {
            if (value < 0) value = 0;
            startSceneDuration = (float)Math.Round(value, 1);
        }
    }

    public virtual float EndSceneDuration
    {
        get => endSceneDuration;
        set
        {
            if (value < 0) value = 0;
            endSceneDuration = (float)Math.Round(value, 1);
        }
    }
    
    [NonNullable]
    public virtual string StartSceneName { get; set; } = string.Empty;
    
    [NonNullable]
    public virtual string GameSceneName { get; set; } = string.Empty;
    
    [NonNullable]
    public virtual string EndSceneName { get; set; } = string.Empty;

    // This is called whenever BSIPA reads the config from disk (including when file changes are detected).
    public virtual void OnReload()
    {
        TryAddCurrentNames(StartSceneName, GameSceneName, EndSceneName);
    }

    // todo: replace this with something else (probably handled by consumer)

    /// <summary>
    /// Call this to force BSIPA to update the config file. This is also called by BSIPA if it detects the file was modified.
    /// </summary>
    public virtual void Changed()
    {
        // Do stuff when the config is changed.
        TryAddCurrentNames(StartSceneName, GameSceneName, EndSceneName);
        RefreshDropdowns();
    }
    
    [UIComponent("StartSceneDropdown"), Ignore] private readonly DropDownListSetting startSceneDropDown = null!;
    [UIComponent("GameSceneDropdown"), Ignore] private readonly DropDownListSetting gameSceneDropdown = null!;
    [UIComponent("EndSceneDropdown"), Ignore] private readonly DropDownListSetting endSceneDropdown = null!;
    private IEnumerable<DropDownListSetting> Dropdowns => [startSceneDropDown, gameSceneDropdown, endSceneDropdown];

    public void UpdateSceneOptions(IEnumerable<string> newOptions)
    {
        SceneSelectOptions.Clear();
        SceneSelectOptions.Add(string.Empty);
        foreach (var name in newOptions.Distinct())
        {
            SceneSelectOptions.Add(name);
        }
        TryAddCurrentNames(StartSceneName, GameSceneName, EndSceneName);
        RefreshDropdowns();
    }

    private void TryAddCurrentNames(params string[] sceneNames)
    {
        foreach (var name in sceneNames.Distinct().Where(n => !SceneSelectOptions.Contains(n)))
        {
            SceneSelectOptions.Add(name);
        }
    }
    
    private void RefreshDropdowns()
    {
        foreach (var dropDown in Dropdowns)
        {
            dropDown.Dropdown.ReloadData();
            dropDown.ReceiveValue();
        }
    }
    
    public string FloatToSeconds(float val) => $"{Math.Round(val, 1)}s";

    [UIValue("SceneSelectOptions"), Ignore]
    public List<object> SceneSelectOptions = new List<object>() { string.Empty };
}