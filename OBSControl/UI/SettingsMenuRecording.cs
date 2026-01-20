using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using OBSControl.Models;

namespace OBSControl.UI;

internal class SettingsMenuRecording : INotifyPropertyChanged
{
    private readonly PluginConfig pluginConfig;

    public SettingsMenuRecording(PluginConfig pluginConfig)
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

    public Array RestartActionChoices = Enum.GetNames(typeof(RestartAction));
    public string RestartAction
    {
        get => pluginConfig.RestartAction.ToString();
        set
        {
            pluginConfig.RestartAction = Enum.TryParse(value, out RestartAction x) ? x : default;
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
    public string RestartActionFormatter(string value) => !Enum.TryParse(value, out RestartAction x) ? string.Empty
        : x switch
        {
            Models.RestartAction.StopRecording => "Stop Recording",
            Models.RestartAction.ContinueRecording => "Continue Recording",
            Models.RestartAction.RestartRecording => "Restart Recording",
            _ => throw new ArgumentOutOfRangeException()
        };
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new(propertyName));
    }
}