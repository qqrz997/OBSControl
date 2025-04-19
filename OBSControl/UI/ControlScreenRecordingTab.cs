using OBSControl.UI.Formatters;

namespace OBSControl.UI;

internal class ControlScreenRecordingTab
{
    public ControlScreenRecordingTab(BoolFormatter boolFormatter)
    {
        BoolFormatter = boolFormatter;
    }
    
    public BoolFormatter BoolFormatter { get; }
    
    public bool IsRecording { get; set; } = false;
    public bool IsNotRecording => !IsRecording;
    
    public string RecordingTextColor { get; set; } = "white";
    
    public int RecordingOutputFrames { get; set; } = 6;
    
    public int OutputSkippedFrames { get; set; } = 7;
    
    public bool RecordButtonInteractable  { get; set; } = true;
    
    public string CurrentScene { get; set; } = "Scene";

    public int FreeDiskSpace { get; set; } = 1024;
    
    public bool EnableAutoRecord { get; set; } = true;
}