using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using OBSControl.Managers;
using OBSControl.UI.Formatters;
using OBSWebsocketDotNet;
using Zenject;

namespace OBSControl.UI;

internal class ControlScreenMainTab : IInitializable, IDisposable, INotifyPropertyChanged
{
    private readonly ObsManager obsManager;
    
    public ControlScreenMainTab(ObsManager obsManager, BoolFormatter boolFormatter)
    {
        this.obsManager = obsManager;
        
        BoolFormatter = boolFormatter;
    }

    public void Initialize()
    {
        obsManager.ConnectionStateChanged += ObsConnectionStateChanged;
        obsManager.Obs.SceneChanged += ObsSceneChanged;
        obsManager.HeartBeat += ObsHeartBeat;
    }

    public void Dispose()
    {
        obsManager.ConnectionStateChanged -= ObsConnectionStateChanged;
        obsManager.Obs.SceneChanged -= ObsSceneChanged;
        obsManager.HeartBeat -= ObsHeartBeat;
    }

    private void ObsConnectionStateChanged(bool obj)
    {
        PropertyChanged(this, new(nameof(IsConnected)));
        PropertyChanged(this, new(nameof(ConnectedTextColor)));
    }

    private void ObsSceneChanged(object sender, SceneChangeEventArgs e)
    {
        CurrentScene = e.NewSceneName;
    }

    private void ObsHeartBeat(HeartBeatEventArgs e)
    {
        RenderMissedFrames = e.Stats.RenderMissedFrames;
    }

    private bool connectButtonInteractable = true;
    private string connectButtonText = "Connect";
    private string currentScene = "SET ME";
    private int renderMissedFrames;

    public BoolFormatter BoolFormatter { get; }

    public bool IsConnected => obsManager.Obs.IsConnected;
    public bool IsRecording { get; set; } = false;
    public bool IsNotRecording => !IsRecording;
    public bool IsStreaming { get; set; } = false;
    public bool IsNotStreaming => !IsStreaming;

    public bool ConnectButtonInteractable
    {
        get => connectButtonInteractable;
        set
        {
            connectButtonInteractable = value;
            OnPropertyChanged();
        }
    }

    public string ConnectButtonText
    {
        get => connectButtonText;
        set
        {
            connectButtonText = value;
            OnPropertyChanged();
        }
    }
    
    public string RecordingTextColor => IsRecording ? "green" : "red";
    
    public string StreamingTextColor => IsStreaming ? "green" : "red";

    public string ConnectedTextColor => IsConnected ? "green" : "red";

    public string CurrentScene
    {
        get => currentScene;
        set
        {
            currentScene = value;
            OnPropertyChanged();
        }
    }

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
        ConnectButtonInteractable = false;
        try
        {
            if (IsConnected)
            {
                obsManager.Obs.Disconnect();
            }
            else
            {
                ConnectButtonText = "Connecting";
                await obsManager.TryConnect();
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Warn($"Error {(IsConnected ? "disconnecting from " : "connecting to ")} OBS: {ex.Message}");
            Plugin.Log.Debug(ex);
            ConnectButtonText = "Error";
        }

        ConnectButtonText = IsConnected ? "Disconnect" : "Connect";
        await Task.Delay(2500);
        ConnectButtonInteractable = true;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged(this, new(propertyName));
    }
}