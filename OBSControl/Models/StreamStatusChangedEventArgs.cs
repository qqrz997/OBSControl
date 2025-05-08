using System;
using OBSWebsocketDotNet.Types;

namespace OBSControl.Models;

internal class StreamStatusChangedEventArgs(OutputStatus status, long bitrate) : EventArgs
{
    public OutputStatus Status { get; } = status;
    public long Bitrate { get; } = bitrate;
}