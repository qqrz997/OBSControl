using System.Linq;
using OBSControl.Utilities;

namespace OBSControl.Models;

public class LevelWrapper : ILevelData
{
    private readonly BeatmapLevel beatmapLevel;
    private readonly BeatmapKey beatmapKey;
    private readonly BeatmapBasicData beatmapBasicData;
        
    public LevelWrapper(BeatmapLevel beatmapLevel, BeatmapKey beatmapKey)
    {
        this.beatmapLevel = beatmapLevel;
        this.beatmapKey = beatmapKey;
            
        beatmapBasicData = beatmapLevel.GetDifficultyBeatmapData(beatmapKey.beatmapCharacteristic, beatmapKey.difficulty);
    }
        
    public string LevelID => beatmapLevel.levelID;

    public string SongName => beatmapLevel.songName;

    public string SongSubName => beatmapLevel.songSubName;

    public string SongAuthorName => beatmapLevel.songAuthorName;

    public string LevelAuthorName => string.Join(", ", beatmapLevel.allMappers.Concat(beatmapLevel.allLighters));

    public float BeatsPerMinute => beatmapLevel.beatsPerMinute;

    public float SongTimeOffset => beatmapLevel.songTimeOffset;

    public float Shuffle => 0f;

    public float ShufflePeriod => 0f;

    public float PreviewStartTime => beatmapLevel.previewStartTime;

    public float PreviewDuration => beatmapLevel.previewDuration;

    public float SongDuration => beatmapLevel.songDuration;

    public Difficulty Difficulty => beatmapKey.difficulty.ToBeatmapDifficulty();

    public int DifficultyRank => (int)beatmapKey.difficulty;

    public float NoteJumpMovementSpeed => beatmapBasicData.noteJumpMovementSpeed;

    public float NoteJumpStartBeatOffset => beatmapBasicData.noteJumpStartBeatOffset;
}

public enum Difficulty
{
    Easy, Normal, Hard, Expert, ExpertPlus
}