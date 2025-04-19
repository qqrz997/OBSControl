using System;

namespace OBSControl.UI;

internal class RecordingSettingsView
{
    private readonly PluginConfig pluginConfig;

    public RecordingSettingsView(PluginConfig pluginConfig)
    {
        this.pluginConfig = pluginConfig;
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
}