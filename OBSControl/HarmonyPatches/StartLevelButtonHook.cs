using System;
using System.Threading.Tasks;
using IPA.Utilities.Async;
using OBSControl.Managers;
using OBSWebsocketDotNet;
using SiraUtil.Affinity;

namespace OBSControl.HarmonyPatches;

internal class StartLevelButtonHook : IAffinity
{
    private readonly PluginConfig pluginConfig;
    private readonly IOBSWebsocket obsWebsocket;
    private readonly RecordingManager recordingManager;

    public StartLevelButtonHook(
        PluginConfig pluginConfig,
        IOBSWebsocket obsWebsocket,
        RecordingManager recordingManager)
    {
        this.pluginConfig = pluginConfig;
        this.obsWebsocket = obsWebsocket;
        this.recordingManager = recordingManager;
    }
    
    private static bool DelayedStartActive { get; set; }
    private static bool WaitingToStart { get; set; }
    
    [AffinityPrefix]
    [AffinityPatch(typeof(SinglePlayerLevelSelectionFlowCoordinator), nameof(SinglePlayerLevelSelectionFlowCoordinator.StartLevel))]
    public bool Prefix(
        // ReSharper disable once InconsistentNaming
        SinglePlayerLevelSelectionFlowCoordinator __instance,
        Action beforeSceneSwitchCallback,
        bool practice)
    {
        if (!pluginConfig.Enabled)
        {
            return true;
        }

        if (!obsWebsocket.IsConnected)
        {
            Plugin.Log.Warn("Not connected to OBS, skipping StartLevel override.");
            return true;
        }
        
        if (DelayedStartActive && WaitingToStart)
        {
            return false; // Ignore this call to StartLevel
        }

        if (!WaitingToStart && DelayedStartActive) // Done waiting, start the level
        {
            DelayedStartActive = false;
            return true;
        }

        var button = __instance.levelSelectionNavigationController._levelCollectionNavigationController._levelDetailViewController._standardLevelDetailView.actionButton;
        var buttonText = __instance.levelSelectionNavigationController._levelCollectionNavigationController._levelDetailViewController._standardLevelDetailView._actionButtonText;
        var originalButtonText = buttonText.text;
        
        DelayedStartActive = true;
        WaitingToStart = true;
        button.interactable = false;
        buttonText.text = "Starting";

        Task.Run(InitiateRecording);
        
        return false;

        async Task InitiateRecording()
        {
            // Wait until the initial scene is shown before continuing
            await recordingManager.StartRecordingLevel(() =>
            {
                // Run this on the main thread since it interacts with game components
                UnityMainThreadTaskScheduler.Factory.StartNew(StartLevelAfterDelay);
            });
        }
        
        async Task StartLevelAfterDelay()
        {
            await Task.Delay(TimeSpan.FromSeconds(pluginConfig.LevelStartDelay));
            
            WaitingToStart = false;
            button.interactable = true;
            buttonText.text = originalButtonText;
        
            __instance.StartLevel(beforeSceneSwitchCallback, practice);
        }
    }
}