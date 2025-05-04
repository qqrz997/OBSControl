using System;
using System.Collections;
using OBSControl.Managers;
using OBSWebsocketDotNet;
using SiraUtil.Affinity;
using UnityEngine;
using UnityEngine.UI;

namespace OBSControl.HarmonyPatches;

internal class StartLevelButtonHook : IAffinity
{
    private readonly PluginConfig pluginConfig;
    private readonly ICoroutineStarter coroutineStarter;
    private readonly IOBSWebsocket obsWebsocket;
    private readonly RecordingManager recordingManager;

    public StartLevelButtonHook(
        PluginConfig pluginConfig,
        ICoroutineStarter coroutineStarter,
        IOBSWebsocket obsWebsocket,
        RecordingManager recordingManager)
    {
        this.pluginConfig = pluginConfig;
        this.coroutineStarter = coroutineStarter;
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
        
        if (pluginConfig.LevelStartDelay == 0)
        {
            recordingManager.StartRecordingLevel();
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
        coroutineStarter.StartCoroutine(StartLevelAfterDelay(__instance, beforeSceneSwitchCallback, practice, button));
        return false;
    }

    private IEnumerator StartLevelAfterDelay(
        SinglePlayerLevelSelectionFlowCoordinator levelSelectionFlowCoordinator,
        Action beforeSceneSwitchCallback,
        bool practice,
        Button playButton)
    {
        DelayedStartActive = true;
        WaitingToStart = true;
        playButton.interactable = false;
        recordingManager.StartRecordingLevel();
        
        Plugin.Log.Debug($"Delaying level start by {pluginConfig.LevelStartDelay} seconds...");
        yield return new WaitForSeconds(pluginConfig.LevelStartDelay);
        
        WaitingToStart = false;
        playButton.interactable = true;
        
        levelSelectionFlowCoordinator.StartLevel(beforeSceneSwitchCallback, practice);
    }
}