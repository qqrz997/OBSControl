using System.Linq;
using OBSControl.Managers;
using OBSControl.Models;
using SiraUtil.Affinity;
using UnityEngine;

namespace OBSControl.HarmonyPatches;

internal class StandardLevelEndHook : IAffinity
{
    private readonly RecordingManager recordingManager;
    private readonly PlayerDataModel playerDataModel;

    public StandardLevelEndHook(RecordingManager recordingManager, PlayerDataModel playerDataModel)
    {
        this.recordingManager = recordingManager;
        this.playerDataModel = playerDataModel;
    }

    [AffinityPatch(typeof(GameplayLevelSceneTransitionEvents), nameof(GameplayLevelSceneTransitionEvents.HandleStandardLevelDidFinish))]
    public void StandardLevelDidFinish(
        StandardLevelScenesTransitionSetupDataSO standardLevelScenesTransitionSetupData,
        LevelCompletionResults levelCompletionResults)
    {
        var playCount = playerDataModel.playerData.GetOrCreatePlayerLevelStatsData(
            standardLevelScenesTransitionSetupData.beatmapLevel.levelID,
            standardLevelScenesTransitionSetupData.beatmapKey.difficulty,
            standardLevelScenesTransitionSetupData.beatmapKey.beatmapCharacteristic).playCount;
        
        var gameplayModifiersModel = Resources.FindObjectsOfTypeAll<GameplayModifiersModelSO>().First();
        var transformedBeatmapData = standardLevelScenesTransitionSetupData.transformedBeatmapData;
        var gameplayModifiers =  standardLevelScenesTransitionSetupData.gameplayModifiers;
        
        var maxMultipliedScore = ScoreModel.ComputeMaxMultipliedScoreForBeatmap(transformedBeatmapData);
        var modifierParams = gameplayModifiersModel.CreateModifierParamsList(gameplayModifiers);
        var maxModifiedScore = gameplayModifiersModel.GetModifiedScoreForGameplayModifiers(maxMultipliedScore, modifierParams, 1f);
            
        var levelWrapper = new LevelWrapper(
            standardLevelScenesTransitionSetupData.beatmapLevel,
            standardLevelScenesTransitionSetupData.beatmapKey);
        
        var resultsWrapper = new LevelCompletionResultsWrapper(levelCompletionResults, playCount, maxModifiedScore);
        
        recordingManager.OnStandardLevelFinished(levelWrapper, resultsWrapper);
    }
}