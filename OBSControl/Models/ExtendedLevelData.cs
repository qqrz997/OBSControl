using System.Linq;

namespace OBSControl.Models;

internal class ExtendedLevelData
{
    public ExtendedLevelData(BeatmapLevel beatmapLevel, BeatmapKey beatmapKey)
    {
        LevelID = beatmapLevel.levelID;
        SongName = beatmapLevel.songName;
        SongSubName = beatmapLevel.songSubName;
        SongAuthorName = beatmapLevel.songAuthorName;
        LevelAuthorName = string.Join(", ", beatmapLevel.allMappers.Concat(beatmapLevel.allLighters));
        BeatsPerMinute = beatmapLevel.beatsPerMinute;
        SongTimeOffset = beatmapLevel.songTimeOffset;
        PreviewStartTime = beatmapLevel.previewStartTime;
        PreviewDuration = beatmapLevel.previewDuration;
        SongDuration = beatmapLevel.songDuration;
        Difficulty = beatmapKey.difficulty;
        DifficultyRank = (int)beatmapKey.difficulty;
        var beatmapBasicData = beatmapLevel.GetDifficultyBeatmapData(beatmapKey.beatmapCharacteristic, beatmapKey.difficulty);
        NoteJumpMovementSpeed = beatmapBasicData.noteJumpMovementSpeed;
        NoteJumpStartBeatOffset = beatmapBasicData.noteJumpStartBeatOffset;
    }
        
    public string LevelID { get; }
    public string SongName { get; }
    public string SongSubName { get; }
    public string SongAuthorName { get; }
    public string LevelAuthorName { get; }
    public float BeatsPerMinute { get; }
    public float SongTimeOffset { get; }
    public float Shuffle => 0f;
    public float ShufflePeriod => 0f;
    public float PreviewStartTime { get; }
    public float PreviewDuration { get; }
    public float SongDuration { get; }
    public BeatmapDifficulty Difficulty { get; }
    public int DifficultyRank { get; }
    public float NoteJumpMovementSpeed { get; }
    public float NoteJumpStartBeatOffset { get; }
}