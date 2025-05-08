using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using OBSControl.Managers;
using OBSControl.Models;
using OBSControl.UI.Formatters;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using Zenject;

namespace OBSControl.UI;

internal class ControlScreenStreamingTab : IInitializable, IDisposable, INotifyPropertyChanged
{
    private readonly EventManager eventManager;
    private readonly IOBSWebsocket obsWebsocket;
    private readonly StreamingManager streamingManager;
    
    public ControlScreenStreamingTab(
        EventManager eventManager,
        IOBSWebsocket obsWebsocket,
        StreamingManager streamingManager,
        BoolFormatter boolFormatter, TimeFormatter timeFormatter)
    {
        this.eventManager = eventManager;
        this.obsWebsocket = obsWebsocket;
        this.streamingManager = streamingManager;
        
        BoolFormatter = boolFormatter;
        TimeFormatter = timeFormatter;
    }

    public void Initialize()
    {
        eventManager.StreamingStateChanged += StreamingStateChanged;
        eventManager.SceneChanged += SceneChanged;
        streamingManager.StreamStatusChanged += StreamStatusChanged;
        StreamingStateChanged(eventManager.StreamingState);
        SceneChanged(eventManager.CurrentScene);
    }

    public void Dispose()
    {
        eventManager.StreamingStateChanged -= StreamingStateChanged;
        eventManager.SceneChanged -= SceneChanged;
        streamingManager.StreamStatusChanged -= StreamStatusChanged;
    }

    private void StreamStatusChanged(StreamStatusChangedEventArgs status)
    {
        StreamTime = (int)(status.StreamDuration / 1000);
        Bitrate = status.Bitrate / 1048576f;
    }

    private void SceneChanged(string sceneName) => CurrentScene = sceneName;

    private void StreamingStateChanged(OutputState outputState) => IsStreaming = outputState switch
    {
        OutputState.OBS_WEBSOCKET_OUTPUT_STARTED => true,
        OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED => false,
        _ => isStreaming
    };

    public BoolFormatter BoolFormatter { get; }
    public TimeFormatter TimeFormatter { get; }

    private bool isStreaming;
    public bool IsStreaming
    {
        get => isStreaming;
        set
        {
            isStreaming = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StreamingTextColor));
            OnPropertyChanged(nameof(IsNotStreaming));
            StreamButtonText = value ? "Stop" : "Start";
        }
    }
    public bool IsNotStreaming => !isStreaming;
    public string StreamingTextColor => isStreaming ? "green" : "red";

    private bool streamButtonInteractable = true;
    public bool StreamButtonInteractable
    {
        get => streamButtonInteractable;
        set
        {
            streamButtonInteractable = value;
            OnPropertyChanged();
        }
    }

    private string streamButtonText = "Start";
    public string StreamButtonText
    {
        get => streamButtonText;
        set
        {
            streamButtonText = value;
            OnPropertyChanged();
        }
    }

    private int streamTime;
    public int StreamTime
    {
        get => streamTime;
        set
        {
            streamTime = value;
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

    private float strain;
    public float Strain
    {
        get => strain;
        set
        {
            strain = value;
            OnPropertyChanged();
        }
    }

    private string currentScene = string.Empty;
    public string CurrentScene
    {
        get => currentScene;
        set
        {
            currentScene = value;
            OnPropertyChanged();
        }
    }

    private int streamingOutputFrames;
    public int StreamingOutputFrames
    {
        get => streamingOutputFrames;
        set
        {
            streamingOutputFrames = value;
            OnPropertyChanged();
        }
    }

    private int streamingDroppedFrames;
    public int StreamingDroppedFrames
    {
        get => streamingDroppedFrames;
        set
        {
            streamingDroppedFrames = value;
            OnPropertyChanged();
        }
    }

    public async void StreamButtonClicked()
    {
        try
        {
            if (!obsWebsocket.IsConnected) return;
            StreamButtonInteractable = false;
            obsWebsocket.ToggleStream();
            await Task.Delay(2000);
        }
        catch (Exception ex)
        {
            Plugin.Log.Warn($"Error toggling streaming: {ex.Message}");
            Plugin.Log.Debug(ex);
        }
        finally
        {
            StreamButtonInteractable = true;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new(propertyName));
    }
}