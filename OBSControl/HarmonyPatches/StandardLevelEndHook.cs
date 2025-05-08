using System.Linq;
using OBSControl.Managers;
using OBSControl.Models;
using OBSControl.Utilities;
using SiraUtil.Affinity;
using UnityEngine;

namespace OBSControl.HarmonyPatches;

internal class StandardLevelEndHook : IAffinity
{
    private readonly PluginConfig pluginConfig;
    private readonly RecordingManager recordingManager;
    private readonly PlayerDataModel playerDataModel;
    private readonly GameplayModifiersModelSO gameplayModifiersModel;

    public StandardLevelEndHook(
        PluginConfig pluginConfig,
        RecordingManager recordingManager,
        PlayerDataModel playerDataModel,
        PrepareLevelCompletionResults prepareLevelCompletionResults)
    {
        this.pluginConfig = pluginConfig;
        this.recordingManager = recordingManager;
        this.playerDataModel = playerDataModel;
        gameplayModifiersModel = prepareLevelCompletionResults._gameplayModifiersModelSO;
    }

    [AffinityPatch(typeof(GameplayLevelSceneTransitionEvents), nameof(GameplayLevelSceneTransitionEvents.HandleStandardLevelDidFinish))]
    public void StandardLevelDidFinish(
        StandardLevelScenesTransitionSetupDataSO standardLevelScenesTransitionSetupData,
        LevelCompletionResults levelCompletionResults)
    {
        if (!pluginConfig.ShouldAutoStopRecording()) return;

        var playCount = playerDataModel.playerData.GetOrCreatePlayerLevelStatsData(
            standardLevelScenesTransitionSetupData.beatmapLevel.levelID,
            standardLevelScenesTransitionSetupData.beatmapKey.difficulty,
            standardLevelScenesTransitionSetupData.beatmapKey.beatmapCharacteristic).playCount;
        
        var transformedBeatmapData = standardLevelScenesTransitionSetupData.transformedBeatmapData;
        var gameplayModifiers =  standardLevelScenesTransitionSetupData.gameplayModifiers;
        
        var maxMultipliedScore = ScoreModel.ComputeMaxMultipliedScoreForBeatmap(transformedBeatmapData);
        var modifierParams = gameplayModifiersModel.CreateModifierParamsList(gameplayModifiers);
        var maxModifiedScore = gameplayModifiersModel.GetModifiedScoreForGameplayModifiers(maxMultipliedScore, modifierParams, 1f);
            
        var levelWrapper = new ExtendedLevelData(
            standardLevelScenesTransitionSetupData.beatmapLevel,
            standardLevelScenesTransitionSetupData.beatmapKey);
        
        var resultsWrapper = new ExtendedCompletionResults(levelCompletionResults, playCount, maxModifiedScore);
        
        recordingManager.OnStandardLevelFinished(levelWrapper, resultsWrapper);
    }
}