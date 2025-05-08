using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using OBSControl.Managers;
using OBSControl.UI.Formatters;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using Zenject;

namespace OBSControl.UI;

internal class ControlScreenRecordingTab : IInitializable, IDisposable, INotifyPropertyChanged
{
    private readonly IOBSWebsocket obsWebsocket;
    private readonly PluginConfig pluginConfig;
    private readonly EventManager eventManager;
    private readonly RecordingManager recordingManager;
    
    public ControlScreenRecordingTab(
        IOBSWebsocket obsWebsocket,
        PluginConfig pluginConfig,
        EventManager eventManager,
        RecordingManager recordingManager,
        BoolFormatter boolFormatter)
    {
        this.obsWebsocket = obsWebsocket;
        this.pluginConfig = pluginConfig;
        this.eventManager = eventManager;
        this.recordingManager = recordingManager;
        
        BoolFormatter = boolFormatter;
    }

    public void Initialize()
    {
        eventManager.RecordingStateChanged += RecordingStateChanged;
    }

    public void Dispose()
    {
        eventManager.RecordingStateChanged -= RecordingStateChanged;
    }
    
    public BoolFormatter BoolFormatter { get; }

    private bool isRecording;

    public bool IsRecording
    {
        get => isRecording;
        set
        {
            isRecording = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RecordingTextColor));
            OnPropertyChanged(nameof(IsNotRecording));
            RecordButtonText = value ? "Stop" : "Start";
        }
    }
    public bool IsNotRecording => !IsRecording;
    public string RecordingTextColor => IsRecording ? "green" : "red";

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

    public int FreeDiskSpace { get; set; } = 1024;
    
    public string CurrentScene { get; set; } = "Scene";
    
    public int RecordingOutputFrames { get; set; } = 6;
    
    public int OutputSkippedFrames { get; set; } = 7;

    private bool recordButtonInteractable = true;
    public bool RecordButtonInteractable
    {
        get => recordButtonInteractable;
        set
        {
            recordButtonInteractable = value;
            OnPropertyChanged();
        }
    }

    private string recordButtonText = "Start";
    public string RecordButtonText
    {
        get => recordButtonText;
        set
        {
            recordButtonText = value;
            OnPropertyChanged();
        }
    }

    public async void RecordButtonClicked()
    {
        try
        {
            if (!obsWebsocket.IsConnected) return;
            RecordButtonInteractable = false;
            recordingManager.ManualToggleRecording();
            await Task.Delay(2000);
        }
        catch (Exception ex)
        {
            Plugin.Log.Warn($"Error toggling streaming: {ex.Message}");
            Plugin.Log.Debug(ex);
        }
        finally
        {
            RecordButtonInteractable = true;
        }
    }

    private void RecordingStateChanged(OutputState outputState) => IsRecording = outputState switch
    {
        OutputState.OBS_WEBSOCKET_OUTPUT_STARTED => true,
        OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED => false,
        _ => IsRecording
    };
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new(propertyName));
    }
}