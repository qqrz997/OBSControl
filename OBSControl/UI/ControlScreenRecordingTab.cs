using System.ComponentModel;
using System.Runtime.CompilerServices;
using OBSControl.UI.Formatters;

namespace OBSControl.UI;

internal class ControlScreenRecordingTab : INotifyPropertyChanged
{
    public ControlScreenRecordingTab(BoolFormatter boolFormatter)
    {
        BoolFormatter = boolFormatter;
    }
    
    public BoolFormatter BoolFormatter { get; }
    
    public bool IsRecording { get; set; } = false;
    public bool IsNotRecording => !IsRecording;
    
    public bool RecordButtonInteractable  { get; set; } = true;
    
    public string RecordingTextColor { get; set; } = "white";
    
    public bool EnableAutoRecord { get; set; } = true;

    public int FreeDiskSpace { get; set; } = 1024;
    
    public string CurrentScene { get; set; } = "Scene";
    
    public int RecordingOutputFrames { get; set; } = 6;
    
    public int OutputSkippedFrames { get; set; } = 7;
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new(propertyName));
    }
}