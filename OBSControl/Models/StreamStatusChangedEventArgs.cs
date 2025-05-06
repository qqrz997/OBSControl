using System;

namespace OBSControl.Models;

internal class StreamStatusChangedEventArgs(long bitrate, long streamDuration) : EventArgs
{
    public long Bitrate { get; } = bitrate;
    public long StreamDuration { get; } = streamDuration;
}