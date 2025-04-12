using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
#nullable enable
namespace OBSControl
{
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
                    Logger.log?.Debug("GameplayModifersModelSO is null, getting new one");
                    gpModSo = Resources.FindObjectsOfTypeAll<GameplayModifiersModelSO>().FirstOrDefault();
                }
                if (gpModSo == null)
                {
                    Logger.log?.Warn("GameplayModifersModelSO is still null");
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
                if (GameplayCoreSceneSetupData == null) return;
                MaxScore = ScoreModel.ComputeMaxMultipliedScoreForBeatmap(GameplayCoreSceneSetupData.transformedBeatmapData);
                Logger.log?.Debug($"MaxScore: {MaxScore}");
                
                if (GameplayModifiersModel == null) return;
                var gameplayModifierList = GameplayModifiersModel.CreateModifierParamsList(GameplayCoreSceneSetupData.gameplayModifiers);
                MaxModifiedScore = GameplayModifiersModel.GetModifiedScoreForGameplayModifiers(MaxScore, gameplayModifierList, 1);
                Logger.log?.Debug($"MaxModifiedScore: {MaxModifiedScore}");
            }
            catch (Exception ex)
            {
                Logger.log?.Error($"Error getting max scores: {ex}");
                Logger.log?.Debug(ex);
            }
        }
    }
}
