using System;
using OBSWebsocketDotNet.Types;
using OBSWebsocketDotNet.Types.Events;

namespace OBSControl.Models;

internal class RecordStatusChangedEventArgs(RecordingStatus status, long bitrate) : EventArgs
{
    public RecordingStatus Status { get; } = status;
    public long Bitrate { get; } = bitrate;
}