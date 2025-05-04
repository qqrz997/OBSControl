using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using OBSControl.Managers;
using OBSControl.UI.Formatters;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using Zenject;

namespace OBSControl.UI;

internal class ControlScreenMainTab : IInitializable, IDisposable, INotifyPropertyChanged
{
    private readonly ObsManager obsManager;
    private readonly IOBSWebsocket obsWebsocket;
    
    public ControlScreenMainTab(ObsManager obsManager, IOBSWebsocket obsWebsocket, BoolFormatter boolFormatter)
    {
        this.obsManager = obsManager;
        this.obsWebsocket = obsWebsocket;
        
        BoolFormatter = boolFormatter;
    }

    public void Initialize()
    {
        obsManager.ConnectionStateChanged += ObsConnectionStateChanged;
        // obsManager.HeartBeatChanged += ObsHeartBeatChanged;
        obsManager.SceneChanged += ObsSceneChanged;
        obsManager.RecordingStateChanged += ObsRecordingStateChanged;
        obsManager.StreamingStateChanged += ObsStreamingStateChanged;
    }

    public void Dispose()
    {
        obsManager.ConnectionStateChanged -= ObsConnectionStateChanged;
        // obsManager.HeartBeatChanged -= ObsHeartBeatChanged;
        obsManager.SceneChanged -= ObsSceneChanged;
        obsManager.RecordingStateChanged -= ObsRecordingStateChanged;
        obsManager.StreamingStateChanged -= ObsStreamingStateChanged;
    }

    [UIAction("#post-parse")]
    public void PostParse()
    {
        ObsConnectionStateChanged(obsWebsocket.IsConnected);
        // if (obsManager.HeartBeat != null) ObsHeartBeatChanged(obsManager.HeartBeat);
        ObsSceneChanged(obsManager.CurrentScene);
        ObsRecordingStateChanged(obsManager.RecordingState);
        ObsStreamingStateChanged(obsManager.StreamingState);
    }

    public BoolFormatter BoolFormatter { get; }

    private bool isConnected;
    public bool IsConnected
    {
        get => isConnected;
        set
        {
            isConnected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ConnectedTextColor));
            ConnectButtonText = value ? "Disconnect" : "Connect";
        }
    }
    
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
        }
    }
    public bool IsNotRecording => !isRecording;
    
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
        }
    }
    public bool IsNotStreaming => !isStreaming;
    
    public string ConnectedTextColor => isConnected ? "green" : "red";
    public string RecordingTextColor => isRecording ? "green" : "red";
    public string StreamingTextColor => isStreaming ? "green" : "red";


    private bool connectButtonInteractable = true;
    public bool ConnectButtonInteractable
    {
        get => connectButtonInteractable;
        set
        {
            connectButtonInteractable = value;
            OnPropertyChanged();
        }
    }

    private string connectButtonText = "Connect";
    public string ConnectButtonText
    {
        get => connectButtonText;
        set
        {
            connectButtonText = value;
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

    private int renderMissedFrames;
    public int RenderMissedFrames
    {
        get => renderMissedFrames;
        set
        {
            renderMissedFrames = value;
            OnPropertyChanged();
        }
    }

    public async void ConnectButtonClicked()
    {
        try
        {
            ConnectButtonInteractable = false;
            obsManager.ToggleConnect();
        }
        catch (Exception ex)
        {
            Plugin.Log.Warn($"Error {(IsConnected ? "disconnecting from" : "connecting to")} OBS: {ex.Message}");
            Plugin.Log.Debug(ex);
            ConnectButtonText = "Error";
        }
        finally
        {
            await Task.Delay(1000);
            ConnectButtonInteractable = true;
        }
    }

    private void ObsConnectionStateChanged(bool connected) => IsConnected = connected;
    
    private void ObsSceneChanged(string sceneName) => CurrentScene = sceneName;
    
    private void ObsStreamingStateChanged(OutputState outputState) => IsStreaming = outputState switch
    {
        OutputState.OBS_WEBSOCKET_OUTPUT_STARTED => true,
        OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED => false,
        _ => IsStreaming
    };

    private void ObsRecordingStateChanged(OutputState outputState) => IsRecording = outputState switch
    {
        OutputState.OBS_WEBSOCKET_OUTPUT_STARTED => true,
        OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED => false,
        _ => IsRecording
    };

    // private void ObsHeartBeatChanged(HeartBeatEventArgs e) => RenderMissedFrames = e.Stats.RenderMissedFrames;
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new(propertyName));
    }
}