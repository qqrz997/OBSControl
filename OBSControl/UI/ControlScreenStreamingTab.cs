using System;
using System.Collections.Generic;
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

internal class ControlScreenStreamingTab : IInitializable, IDisposable, INotifyPropertyChanged
{
    private readonly ObsManager obsManager;
    
    public ControlScreenStreamingTab(ObsManager obsManager, BoolFormatter boolFormatter, TimeFormatter timeFormatter)
    {
        this.obsManager = obsManager;
        
        BoolFormatter = boolFormatter;
        TimeFormatter = timeFormatter;
    }

    public void Initialize()
    {
        obsManager.StreamingStateChanged += ObsStreamingStateChanged;
        obsManager.SceneChanged += ObsSceneChanged;
        obsManager.HeartBeatChanged += ObsHeartBeatChanged;
        obsManager.StreamStatusChanged += ObsStreamingStatusChanged;
    }

    private void ObsStreamingStatusChanged(StreamStatusEventArgs e)
    {
        StreamTime = e.TotalStreamTime;
    }

    private void ObsHeartBeatChanged(HeartBeatEventArgs e)
    {
        
    }

    private void ObsSceneChanged(string sceneName) => CurrentScene = sceneName;

    private void ObsStreamingStateChanged(OutputState outputState) => IsStreaming = outputState switch
    {
        OutputState.Started => true,
        OutputState.Stopped => false,
        _ => isStreaming
    };

    public void Dispose()
    {
        obsManager.StreamingStateChanged -= ObsStreamingStateChanged;
        obsManager.SceneChanged -= ObsSceneChanged;
        obsManager.HeartBeatChanged -= ObsHeartBeatChanged;
        obsManager.StreamStatusChanged -= ObsStreamingStatusChanged;
    }

    [UIAction("#post-parse")]
    public void PostParse()
    {
    }
    
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

    private int bitrate;
    public int Bitrate
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
            StreamButtonInteractable = false;
            await obsManager.ToggleStreaming();
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