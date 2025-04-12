using System;

namespace OBSControl.Wrappers;

public class LevelCompletionResultsWrapper : ILevelCompletionResults
{
    private LevelCompletionResults _results;
    public LevelCompletionResultsWrapper(LevelCompletionResults results, int playCount, int maxModifiedScore)
    {
        _results = results;
        GameplayModifiers = new GameplayModifiersWrapper(results.gameplayModifiers);
        PlayCount = playCount;
        MaxModifiedScore = maxModifiedScore;
        if (MaxModifiedScore != 0)
        {
            // TODO: check this is the right value
            ScorePercent = (float)results.multipliedScore / MaxModifiedScore * 100f;
        }
    }
    public int PlayCount { get; }
    public int MaxModifiedScore { get; }
    public float ScorePercent { get; }

    public IGameplayModifiers GameplayModifiers { get; }

    public int ModifiedScore => _results.modifiedScore;

    // TODO: check this is the right value
    public int RawScore => _results.multipliedScore;

    public ScoreRank Rank => _results.rank.ToScoreRank();

    public bool FullCombo => _results.fullCombo;

    public LevelEndState LevelEndStateType => _results.levelEndStateType.ToLevelEndState();

    public SongEndAction LevelEndAction => _results.levelEndAction.ToSongEndAction();

    // TODO: check this is the right value
    public int AverageCutScore => (int)Math.Ceiling(_results.averageCutScoreForNotesWithFullScoreScoringType);

    public int GoodCutsCount => _results.goodCutsCount;

    public int BadCutsCount => _results.badCutsCount;

    public int MissedCount => _results.missedCount;

    public int MaxCombo => _results.maxCombo;

    public float EndSongTime => _results.endSongTime;
}