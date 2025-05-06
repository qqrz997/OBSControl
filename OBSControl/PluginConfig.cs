using System;
using System.Runtime.CompilerServices;
using IPA.Config.Stores;
using UnityEngine;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace OBSControl;

internal class PluginConfig
{
    public virtual bool Enabled { get; set; } = true;
    
    public virtual string? WsIpAddress { get; set; } = "192.168.0.0";
    public virtual string? WsPort { get; set; } = "4455";
    public virtual string? ServerPassword { get; set; } = string.Empty;

    public virtual bool UseSceneTransitions { get; set; } = true;

    private float levelStartDelay = 3f;
    public virtual float LevelStartDelay
    {
        get => levelStartDelay;
        set => levelStartDelay = value < 0 ? 0f : (float)Math.Round(value, 1);
    }
    
    private float recordingStopDelay = 4f;
    public virtual float RecordingStopDelay
    {
        get => recordingStopDelay;
        set => recordingStopDelay = value < 0 ? 0f : (float)Math.Round(value, 1);
    }
    
    public virtual string? RecordingFileFormat { get; set; } = "?N{20}-?A{20}_?%<_[?M]><-?F><-?e>";
    
    public virtual string? ReplaceSpacesWith { get; set; } = "_";
    public virtual string? InvalidCharacterSubstitute { get; set; } = "_";

    private float startSceneDuration = 1f;
    public virtual float StartSceneDuration
    {
        get => startSceneDuration;
        set => startSceneDuration = value < 0 ? 0f : (float)Math.Round(value, 1);
    }

    private float endSceneDuration = 2f;
    public virtual float EndSceneDuration
    {
        get => endSceneDuration;
        set => endSceneDuration = value < 0 ? 0f : (float)Math.Round(value, 1);
    }

    private float endSceneDelay;
    public float EndSceneDelay
    {
        get => endSceneDelay;
        set => endSceneDelay = value < 0 ? 0f : (float)Math.Round(value, 1);
    }
    
    public virtual string StartSceneName { get; set; } = string.Empty;
    public virtual string GameSceneName { get; set; } = string.Empty;
    public virtual string EndSceneName { get; set; } = string.Empty;
    public virtual string PostRecordSceneName { get; set; } = string.Empty;
    
    // Control screen config
    public virtual bool ShowControlScreen { get; set; } = true;
    public virtual bool ControlScreenLocked { get; set; } = true;
    public virtual Vector3 ControlScreenPosition { get; set; } = new (0f, 1f, 1f);
    public virtual Quaternion ControlScreenRotation { get; set; } = Quaternion.identity;
}