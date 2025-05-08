using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OBSControl.UI;

internal class RecordingSettingsView : INotifyPropertyChanged
{
    private readonly PluginConfig pluginConfig;

    public RecordingSettingsView(PluginConfig pluginConfig)
    {
        this.pluginConfig = pluginConfig;
    }

    public bool EnableAutoRecord
    {
        get => pluginConfig.AutoRecord;
        set
        {
            pluginConfig.AutoRecord = value;
            OnPropertyChanged();
        }
    }

    public bool AutoStopRecord
    {
        get => pluginConfig.AutoStopRecord;
        set
        {
            pluginConfig.AutoStopRecord = value;
            OnPropertyChanged();
        }
    }

    public bool UseSceneTransitions
    {
        get => pluginConfig.UseSceneTransitions;
        set => pluginConfig.UseSceneTransitions = value;
    }

    public float LevelStartDelay
    {
        get => pluginConfig.LevelStartDelay;
        set => pluginConfig.LevelStartDelay = value;
    }

    public float RecordingStopDelay
    {
        get => pluginConfig.RecordingStopDelay;
        set => pluginConfig.RecordingStopDelay = value;
    }

    public string FloatToSeconds(float val) => $"{Math.Round(val, 1)}s";
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new(propertyName));
    }
}