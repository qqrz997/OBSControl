using System;
using System.Linq;
using UnityEngine;

namespace OBSControl;

public static class GameStatus
{
    private static GameplayModifiersModelSO? gpModSo;
    public static int MaxScore;
    public static int MaxModifiedScore;

    private static GameplayCoreSceneSetupData? GameplayCoreSceneSetupData =>
        !BS_Utils.Plugin.LevelData.IsSet ? null
            : BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData;

    public static BeatmapLevel? BeatmapLevel => GameplayCoreSceneSetupData?.beatmapLevel;
        
    public static BeatmapKey? BeatmapKey => GameplayCoreSceneSetupData?.beatmapKey;

    public static IReadonlyBeatmapData? BeatmapData => GameplayCoreSceneSetupData?.transformedBeatmapData;
        
    public static GameplayModifiersModelSO? GameplayModifiersModel
    {
        get
        {
            if (gpModSo == null)
            {
                Plugin.Log.Debug("GameplayModifersModelSO is null, getting new one");
                gpModSo = Resources.FindObjectsOfTypeAll<GameplayModifiersModelSO>().FirstOrDefault();
            }
            if (gpModSo == null)
            {
                Plugin.Log.Warn("GameplayModifersModelSO is still null");
            }
            //else
            //    Logger.Debug("Found GameplayModifersModelSO");
            return gpModSo;
        }
    }

    public static void Setup()
    {
        try
        {
            // TODO: Handle no-fail properly
            if (BeatmapData != null)
            {
                MaxScore = ScoreModel.ComputeMaxMultipliedScoreForBeatmap(BeatmapData);
                Plugin.Log.Debug($"MaxScore: {MaxScore}");
            }

            if (GameplayModifiersModel != null && GameplayCoreSceneSetupData != null)
            {
                var gameplayModifierList = GameplayModifiersModel.CreateModifierParamsList(GameplayCoreSceneSetupData.gameplayModifiers);
                MaxModifiedScore = GameplayModifiersModel.GetModifiedScoreForGameplayModifiers(MaxScore, gameplayModifierList, 1);
                Plugin.Log.Debug($"MaxModifiedScore: {MaxModifiedScore}");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error getting max scores: {ex}");
            Plugin.Log.Debug(ex);
        }
    }
}