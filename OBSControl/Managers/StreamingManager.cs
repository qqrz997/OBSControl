using System;
using System.Threading;
using System.Threading.Tasks;
using OBSControl.Models;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using Zenject;

namespace OBSControl.Managers;

internal class StreamingManager : IInitializable, IDisposable
{
    private readonly EventManager eventManager;
    private readonly IOBSWebsocket obsWebsocket;

    public StreamingManager(EventManager eventManager, IOBSWebsocket obsWebsocket)
    {
        this.eventManager = eventManager;
        this.obsWebsocket = obsWebsocket;
    }
    
    public event Action<StreamStatusChangedEventArgs>? StreamStatusChanged;
    
    private CancellationTokenSource streamStatusTokenSource = new();
    private long lastBytesSent;

    public void Initialize()
    {
        eventManager.StreamingStateChanged += StreamingStateChanged;
    }

    public void Dispose()
    {
        eventManager.StreamingStateChanged -= StreamingStateChanged;
    }
    
    private void StreamingStateChanged(OutputState outputState)
    {
        switch (outputState)
        {
            case OutputState.OBS_WEBSOCKET_OUTPUT_STARTED:
                StartPollingStreamStatus();
                break;
            case OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED:
                StopPollingStreamStatus();
                break;
            case OutputState.OBS_WEBSOCKET_OUTPUT_STARTING:
            case OutputState.OBS_WEBSOCKET_OUTPUT_STOPPING:
            case OutputState.OBS_WEBSOCKET_OUTPUT_PAUSED:
            case OutputState.OBS_WEBSOCKET_OUTPUT_RESUMED:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(outputState), outputState, null);
        }
    }

    private void StartPollingStreamStatus()
    {
        StopPollingStreamStatus();
        Task.Run(() => RepeatPollStreamStatus(streamStatusTokenSource.Token));
    }

    private void StopPollingStreamStatus()
    {
        streamStatusTokenSource.Cancel();
        streamStatusTokenSource.Dispose();
        streamStatusTokenSource = new();
    }
    
    private async Task RepeatPollStreamStatus(CancellationToken token)
    {
        const int interval = 2500;
        
        try
        {
            while (!token.IsCancellationRequested && obsWebsocket.IsConnected)
            {
                var status = obsWebsocket.GetStreamStatus();
                var (bytes, duration) = (status.BytesSent, status.Duration);
                
                var bitrate = (bytes - lastBytesSent) / (interval / 1000) * 8;
                lastBytesSent = bytes;

                StreamStatusChanged?.Invoke(new(bitrate, duration));
                await Task.Delay(interval, token);
            }
        }
        catch (OperationCanceledException) { }
    }
}