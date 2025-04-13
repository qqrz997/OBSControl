using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IPA.Utilities.Async;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using UnityEngine;

namespace OBSControl;

/// <summary>
/// Monobehaviours (scripts) are added to GameObjects.
/// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
/// </summary>
public class OBSController
    : MonoBehaviour
{
    private OBSWebsocket? _obs;
    public OBSWebsocket? Obs
    {
        get => _obs;
        protected set
        {
            if (_obs == value)
                return;
            Plugin.Log.Info($"obs.set");
            if (_obs != null)
            {

            }
            _obs = value;
        }
    }

    //private static float PlayerHeight;

//        private PlayerSpecificSettings _playerSettings;
//        private PlayerSpecificSettings PlayerSettings
//        {
//            get
//            {
//                if (_playerSettings == null)
//                {
//                    _playerSettings = GameStatus.gameSetupData?.playerSpecificSettings;
//                    if (_playerSettings != null)
//                    {
//                        Logger.Log.Debug("Found PlayerSettings");
//                    }
//                    else
//                        Logger.Log.Warn($"Unable to find PlayerSettings");
//                }
//#if DEBUG
//                else
//                    Logger.Log.Debug("PlayerSettings already exists, don't need to find it");
//#endif
//                return _playerSettings;
//            }
//        }

    private PlayerDataModel? _playerData;
    public PlayerDataModel? PlayerData
    {
        get
        {
            if (_playerData == null)
            {
                _playerData = Resources.FindObjectsOfTypeAll<PlayerDataModel>().FirstOrDefault();
                if (_playerData != null)
                {
                    Plugin.Log.Debug("Found PlayerData");
                }
                else
                    Plugin.Log.Warn($"Unable to find PlayerData");
            }
#if DEBUG
                else
                    Plugin.Log.Debug("PlayerData already exists, don't need to find it");
#endif
            return _playerData;
        }
    }

    private bool OnConnectTriggered = false;
    public string? RecordingFolder;

    public static OBSController? instance { get; private set; }
    public bool IsConnected => Obs?.IsConnected ?? false;

    private PluginConfig Config => Plugin.Config;
    public event EventHandler? DestroyingObs;

    #region Setup/Teardown

    private void CreateObsInstance()
    {
        Plugin.Log.Debug("CreateObsInstance()");
        var newObs = new OBSWebsocket();
        //newObs. = new TimeSpan(0, 0, 30);
        newObs.Connected += OnConnect;
        newObs.Disconnected += OnDisconnect;
        newObs.StreamingStateChanged += Obs_StreamingStateChanged;
        newObs.StreamStatus += Obs_StreamStatus;
        newObs.SceneListChanged += OnObsSceneListChanged;
        Obs = newObs;
        Plugin.Log.Debug("CreateObsInstance finished");
    }

    private void OnDisconnect(object sender, EventArgs e)
    {
    }

    private HashSet<EventHandler<OutputState>> _recordingStateChangedHandlers = new HashSet<EventHandler<OutputState>>();
    public event EventHandler<OutputState> RecordingStateChanged
    {
        add
        {
            bool firstSubscriber = _recordingStateChangedHandlers.Count == 0;
            _recordingStateChangedHandlers.Add(value);
            if (firstSubscriber && _recordingStateChangedHandlers != null && _obs != null)
                _obs.RecordingStateChanged += OnRecordingStateChanged;
        }
        remove
        {
            _recordingStateChangedHandlers.Remove(value);
            if (_recordingStateChangedHandlers.Count == 0 && _obs != null)
                _obs.RecordingStateChanged -= OnRecordingStateChanged;
        }
    }

    protected void OnRecordingStateChanged(object sender, OutputStateChangedEventArgs outputState)
    {
        foreach (var handler in _recordingStateChangedHandlers)
        {
            handler.Invoke(this, outputState.OutputState);
        }
    }
    private void DestroyObsInstance(OBSWebsocket? target)
    {
        if (target == null)
            return;
        Plugin.Log.Debug("Disconnecting from obs instance.");
        DestroyingObs?.Invoke(this, null);
        if (target.IsConnected)
        {
            //target.Api.SetFilenameFormatting(DefaultFileFormat);
            target.Disconnect();
        }
        target.Connected -= OnConnect;
        target.RecordingStateChanged -= OnRecordingStateChanged;
        target.StreamingStateChanged -= Obs_StreamingStateChanged;
        target.StreamStatus -= Obs_StreamStatus;
        target.SceneListChanged -= OnObsSceneListChanged;
    }

    public string? lastTryConnectMessage;
    public async Task<bool> TryConnect()
    {
        string message;
        string? serverAddress = Config.ServerAddress;
        if(serverAddress == null || serverAddress.Length == 0)
        {
            Plugin.Log.Error($"ServerAddress cannot be null or empty.");
            return false;
        }
        if (Obs != null && !Obs.IsConnected)
        {
            try
            {
                await Obs.Connect(serverAddress, Config.ServerPassword).ConfigureAwait(false);
                message = $"Finished attempting to connect to {Config.ServerAddress}";
                if (message != lastTryConnectMessage)
                {
                    Plugin.Log.Info(message);
                    lastTryConnectMessage = message;
                }
            }
            catch (AuthFailureException)
            {
                message = $"Authentication failed connecting to server {Config.ServerAddress}.";
                if (message != lastTryConnectMessage)
                {
                    Plugin.Log.Info(message);
                    lastTryConnectMessage = message;
                }
                return false;
            }
            catch (ErrorResponseException ex)
            {
                message = $"Failed to connect to server {Config.ServerAddress}: {ex.Message}.";
                if (message != lastTryConnectMessage)
                {
                    Plugin.Log.Info(message);
                    lastTryConnectMessage = message;
                }
                Plugin.Log.Debug(ex);
                return false;
            }
            catch (Exception ex)
            {
                message = $"Failed to connect to server {Config.ServerAddress}: {ex.Message}.";
                if (message != lastTryConnectMessage)
                {
                    Plugin.Log.Info(message);
                    Plugin.Log.Debug(ex);
                    lastTryConnectMessage = message;
                }
                return false;
            }
            if (Obs.IsConnected)
                Plugin.Log.Info($"Connected to OBS @ {Config.ServerAddress}");
        }
        else
            Plugin.Log.Info("TryConnect: OBS is already connected.");
        return Obs?.IsConnected ?? false;
    }

    private async Task RepeatTryConnect()
    {
        OBSWebsocket? obs = Obs;
        if(obs == null)
        {
            Plugin.Log.Error($"Obs instance is null in RepeatTryConnect()");
            return;
        }
        try
        {
            if (string.IsNullOrEmpty(Plugin.Config.ServerAddress))
            {
                Plugin.Log.Error("The ServerAddress in the config is null or empty. Unable to connect to OBS.");
                return;
            }
            Plugin.Log.Info($"Attempting to connect to {Config.ServerAddress}");
            while (!(await TryConnect().ConfigureAwait(false)))
            {
                await Task.Delay(5000).ConfigureAwait(false);
            }

            Plugin.Log.Info($"OBS {(await obs.GetVersion().ConfigureAwait(false)).OBSStudioVersion} is connected.");
            Plugin.Log.Info($"OnConnectTriggered: {OnConnectTriggered}");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error in RepeatTryConnect: {ex.Message}");
            Plugin.Log.Debug(ex);
        }
    }

    #endregion

    #region Event Handlers
    private async void OnConnect(object sender, EventArgs e)
    {
        OBSWebsocket? obs = _obs;
        if (obs == null) return;
        OnConnectTriggered = true;
        Plugin.Log.Info($"OnConnect: Connected to OBS.");
        try
        {
            string[] availableScenes = (await obs.GetSceneList().ConfigureAwait(false)).Scenes.Select(s => s.Name).ToArray();
            await UnityMainThreadTaskScheduler.Factory.StartNew(() =>
            {
                Plugin.Config.UpdateSceneOptions(availableScenes);
            });
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error getting scene list: {ex.Message}");
            Plugin.Log.Debug(ex);
        }
    }

    private async void OnObsSceneListChanged(object sender, EventArgs e)
    {
        OBSWebsocket? obs = _obs;
        if (obs == null) return;
        try
        {
            string[] availableScenes = (await obs.GetSceneList().ConfigureAwait(false)).Scenes.Select(s => s.Name).ToArray();
            Plugin.Log.Info($"OBS scene list changed: {string.Join(", ", availableScenes)}");
            await UnityMainThreadTaskScheduler.Factory.StartNew(() =>
            {
                Plugin.Config.UpdateSceneOptions(availableScenes);
            });
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error getting scene list: {ex.Message}");
            Plugin.Log.Debug(ex);
        }
    }

    private void Obs_StreamingStateChanged(object sender, OutputStateChangedEventArgs e)
    {
        Plugin.Log.Info($"Streaming State Changed: {e.OutputState.ToString()}");
    }


    private void Obs_StreamStatus(object sender, StreamStatusEventArgs status)
    {
        Plugin.Log.Info($"Stream Time: {status.TotalStreamTime.ToString()} sec");
        Plugin.Log.Info($"Bitrate: {(status.KbitsPerSec / 1024f).ToString("N2")} Mbps");
        Plugin.Log.Info($"FPS: {status.FPS.ToString()} FPS");
        Plugin.Log.Info($"Strain: {(status.Strain * 100).ToString()} %");
        Plugin.Log.Info($"DroppedFrames: {status.DroppedFrames.ToString()} frames");
        Plugin.Log.Info($"TotalFrames: {status.TotalFrames.ToString()} frames");
    }

    #endregion

    #region Monobehaviour Messages
    /// <summary>
    /// Only ever called once, mainly used to initialize variables.
    /// </summary>
    private void Awake()
    {
        Plugin.Log.Debug("OBSController Awake()");
        if (instance != null)
        {
            GameObject.DestroyImmediate(this);
            return;
        }
        GameObject.DontDestroyOnLoad(this);
        instance = this;
        CreateObsInstance();
    }

    /// <summary>
    /// Only ever called once on the first frame the script is Enabled. Start is called after every other script's Awake() and before Update().
    /// </summary>
    private void Start()
    {
        Plugin.Log.Debug("OBSController Start()");
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        RepeatTryConnect();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    }

    /// <summary>
    /// Called when the script becomes enabled and active
    /// </summary>
    private void OnEnable()
    {
        OBSComponents.RecordingController.instance?.gameObject.SetActive(true);
    }

    /// <summary>
    /// Called when the script becomes disabled or when it is being destroyed.
    /// </summary>
    private void OnDisable()
    {
        OBSComponents.RecordingController.instance?.gameObject.SetActive(false);
    }

    /// <summary>
    /// Called when the script is being destroyed.
    /// </summary>
    private void OnDestroy()
    {
        instance = null;
        if (OBSComponents.RecordingController.instance != null)
        {
            Destroy(OBSComponents.RecordingController.instance);
        }
        DestroyObsInstance(Obs);
    }
    #endregion
}