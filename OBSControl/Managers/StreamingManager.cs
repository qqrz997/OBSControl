using System;
using System.Threading;
using System.Threading.Tasks;
using OBSWebsocketDotNet.Types;
using Zenject;

namespace OBSControl.Managers;

internal class StreamingManager : IInitializable, IDisposable
{
    private readonly ObsManager obsManager;

    public StreamingManager(ObsManager obsManager)
    {
        this.obsManager = obsManager;
    }
    
    public event Action<OutputStatus>? StreamStatusChanged;
    
    private CancellationTokenSource streamStatusTokenSource = new();

    public void Initialize()
    {
        obsManager.StreamingStateChanged += ObsStreamingStateChanged;
    }

    public void Dispose()
    {
        obsManager.StreamingStateChanged -= ObsStreamingStateChanged;
    }
    
    private void ObsStreamingStateChanged(OutputState outputState)
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
        try
        {
            while (!token.IsCancellationRequested)
            {
                var status = obsManager.Obs.GetStreamStatus();
                StreamStatusChanged?.Invoke(status);
                await Task.Delay(2000, token);
            }
        }
        catch (OperationCanceledException) { }
    }
}