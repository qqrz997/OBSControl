using System;

namespace OBSControl.Models;

internal class ExtendedCompletionResults
{
    public ExtendedCompletionResults(LevelCompletionResults results, int playCount, int maxModifiedScore)
    {
        PlayCount = playCount;
        MaxModifiedScore = maxModifiedScore;
        ScorePercent = maxModifiedScore == 0 ? 0 : (float)results.modifiedScore / maxModifiedScore * 100f;
        GameplayModifiers = results.gameplayModifiers;
        ModifiedScore = results.modifiedScore;
        RawScore = results.multipliedScore;
        Rank = results.rank;
        FullCombo = results.fullCombo;
        LevelEndStateType = results.levelEndStateType;
        LevelEndAction = results.levelEndAction;
        AverageCutScore = (int)Math.Round(results.averageCutScoreForNotesWithFullScoreScoringType);
        GoodCutsCount = results.goodCutsCount;
        BadCutsCount = results.badCutsCount;
        MissedCount = results.missedCount;
        MaxCombo = results.maxCombo;
        EndSongTime = results.endSongTime;
    }

    public int PlayCount { get; }
    public int MaxModifiedScore { get; }
    public float ScorePercent { get; }
    public GameplayModifiers GameplayModifiers { get; }
    public int ModifiedScore { get; }
    public int RawScore { get; }
    public RankModel.Rank Rank { get; }
    public bool FullCombo { get; }
    public LevelCompletionResults.LevelEndStateType LevelEndStateType { get; }
    public LevelCompletionResults.LevelEndAction LevelEndAction { get; }
    public int AverageCutScore { get; }
    public int GoodCutsCount { get; }
    public int BadCutsCount { get; }
    public int MissedCount { get; }
    public int MaxCombo { get; }
    public float EndSongTime { get; }
}