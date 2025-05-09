using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using OBSControl.Managers;
using OBSControl.Models;
using OBSControl.UI.Formatters;
using OBSControl.Utilities;
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
        BoolFormatter boolFormatter, TimeFormatter timeFormatter)
    {
        this.obsWebsocket = obsWebsocket;
        this.pluginConfig = pluginConfig;
        this.eventManager = eventManager;
        this.recordingManager = recordingManager;
        
        BoolFormatter = boolFormatter;
        TimeFormatter = timeFormatter;
    }

    public void Initialize()
    {
        eventManager.RecordingStateChanged += RecordingStateChanged;
        eventManager.DriveSpaceUpdated += DriveSpaceUpdated;
        eventManager.SceneChanged += SceneChanged;
        recordingManager.RecordStatusChanged += RecordStatusChanged;
        RecordingStateChanged(eventManager.RecordingState);
        DriveSpaceUpdated(eventManager.DriveSpace);
        SceneChanged(eventManager.CurrentScene);
        if (recordingManager.CurrentRecordingStatus != null) RecordStatusChanged(recordingManager.CurrentRecordingStatus);
    }

    public void Dispose()
    {
        eventManager.RecordingStateChanged -= RecordingStateChanged;
        eventManager.DriveSpaceUpdated -= DriveSpaceUpdated;
        eventManager.SceneChanged -= SceneChanged;
    }
    
    public BoolFormatter BoolFormatter { get; }
    public TimeFormatter TimeFormatter { get; }

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

    private string freeDiskSpace = "Unknown";
    public string FreeDiskSpace
    {
        get => freeDiskSpace;
        set
        {
            freeDiskSpace = value;
            OnPropertyChanged();
        }
    }
    
    private string currentScene = "Unknown";
    public string CurrentScene
    {
        get => currentScene;
        set
        {
            currentScene = value;
            OnPropertyChanged();
        }
    }

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

    private int recordTime;
    public int RecordTime
    {
        get => recordTime;
        set
        {
            recordTime = value;
            OnPropertyChanged();
        }
    }
    
    private float bitrate;
    public float Bitrate
    {
        get => bitrate;
        set
        {
            bitrate = value;
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

    private void DriveSpaceUpdated(long driveSpace)
    {
        FreeDiskSpace = driveSpace.FormatBytes();
    }

    private void SceneChanged(string sceneName)
    {
        CurrentScene = sceneName;
    }

    private void RecordStatusChanged(RecordStatusChangedEventArgs args)
    {
        RecordTime = (int)(args.Status.RecordingDuration / 1000);
        Bitrate = args.Bitrate / 1048576f;
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new(propertyName));
    }
}