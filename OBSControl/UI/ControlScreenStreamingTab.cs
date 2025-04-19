using OBSControl.UI.Formatters;

namespace OBSControl.UI;

internal class ControlScreenStreamingTab
{
    public ControlScreenStreamingTab(BoolFormatter boolFormatter, TimeFormatter timeFormatter)
    {
        BoolFormatter = boolFormatter;
        TimeFormatter = timeFormatter;
    }
    
    public BoolFormatter BoolFormatter { get; }
    public TimeFormatter TimeFormatter { get; }
    
    public bool IsStreaming { get; set; } = false;
    public bool IsNotStreaming => !IsStreaming;
    
    public bool StreamButtonInteractable  { get; set; } = true;
    
    public int StreamingOutputFrames { get; set; } = 4;
    
    public int StreamingDroppedFrames { get; set; } = 5;

    public string Strain { get; set; } = "Sample";

    public float StreamTime { get; set; } = 1203.2f;
    
    public int Bitrate { get; set; } = 1024;
    
    public string StreamingTextColor { get; set; } = "white";
    
    
    public string CurrentScene { get; set; } = "Scene";
}