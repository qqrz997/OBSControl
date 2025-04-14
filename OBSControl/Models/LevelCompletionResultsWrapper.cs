using System;
using OBSControl.Utilities;

namespace OBSControl.Models;

public class LevelCompletionResultsWrapper : ILevelCompletionResults
{
    public LevelCompletionResultsWrapper(LevelCompletionResults results, int playCount, int maxModifiedScore)
    {
        PlayCount = playCount;
        MaxModifiedScore = maxModifiedScore;
        ScorePercent = maxModifiedScore == 0 ? 0 : (float)results.modifiedScore / maxModifiedScore * 100f;
        GameplayModifiers = new GameplayModifiersWrapper(results.gameplayModifiers);
        ModifiedScore = results.modifiedScore;
        RawScore = results.multipliedScore;
        Rank = results.rank.ToScoreRank();
        FullCombo = results.fullCombo;
        LevelEndStateType = results.levelEndStateType.ToLevelEndState();
        LevelEndAction = results.levelEndAction.ToSongEndAction();
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
    public IGameplayModifiers GameplayModifiers { get; }
    public int ModifiedScore { get; }
    public int RawScore { get; }
    public ScoreRank Rank { get; }
    public bool FullCombo { get; }
    public LevelEndState LevelEndStateType { get; }
    public SongEndAction LevelEndAction { get; }
    public int AverageCutScore { get; }
    public int GoodCutsCount { get; }
    public int BadCutsCount { get; }
    public int MissedCount { get; }
    public int MaxCombo { get; }
    public float EndSongTime { get; }
}