using System;
using System.Collections.Generic;
using System.Text;
using OBSControl.Models;
using static LevelCompletionResults;

namespace OBSControl.Utilities;

internal static class FileRenaming
{
    /// <summary>
    /// Creates a file name string from a base string substituting characters prefixed by '?' with data from the game.
    /// </summary>
    public static string GetFilenameString(
        string? baseString,
        ExtendedLevelData levelData,
        ExtendedCompletionResults levelCompletionResults,
        string? invalidSubstitute = "",
        string? spaceReplacement = null)
    {
        if (levelData == null)
            throw new ArgumentNullException(nameof(levelData), "difficultyBeatmap cannot be null for GetFilenameString.");
        if (levelCompletionResults == null)
            throw new ArgumentNullException(nameof(levelCompletionResults), "levelCompletionResults cannot be null for GetFilenameString.");
        if(string.IsNullOrEmpty(baseString) || baseString == null)
            return string.Empty;
        if (!baseString.Contains("?"))
            return baseString;
        StringBuilder stringBuilder = new StringBuilder(baseString.Length);
        StringBuilder section = new StringBuilder(20);
        bool substituteNext = false;
        bool inProcessingGroup = false; // Group that is skipped if there's no data
        bool ignoreGroup = true; // False if the processingGroup contains data
        for(int i = 0; i < baseString.Length; i++)
        {
            char ch = baseString[i];
            switch (ch)
            {
                case '<':
                    section.Clear();
                    inProcessingGroup = true;
                    continue;
                case '>':
                    inProcessingGroup = false;
                    if (!ignoreGroup && section.Length > 0)
                        stringBuilder.Append(section.ToString());
                    section.Clear();
                    ignoreGroup = true;
                    continue;
                case '?':
                    substituteNext = true;
                    continue;
                default:
                    if (substituteNext)
                    {
                        string? dataString = null;
                        int nextIndex = i + 1;
                        if (nextIndex < baseString.Length && baseString[nextIndex] == '{')
                        {
                            nextIndex++;
                            int lastIndex = baseString.IndexOf('}', nextIndex);
                            if(lastIndex > 0)
                            {
                                dataString = baseString.Substring(nextIndex, lastIndex - nextIndex);
                                i = lastIndex;
                            }
                        }
                        if (inProcessingGroup)
                        {
                            string data;
                            try
                            {
                                data = GetLevelDataString(LevelDataSubstitutions[ch], levelData, levelCompletionResults, dataString);
                            }
                            catch
                            { 
                                data = "INVLD"; 
                            }
                            if (!string.IsNullOrEmpty(data))
                            {
                                ignoreGroup = false;
                                section.Append(data);
                            }
                        }
                        else
                        {
                            try
                            {
                                stringBuilder.Append(GetLevelDataString(LevelDataSubstitutions[ch], levelData, levelCompletionResults, dataString));
                            }
                            catch 
                            { 
                                stringBuilder.Append("INVLD"); 
                            }
                                
                        }
                        substituteNext = false;
                    }
                    else
                    {
                        if (inProcessingGroup)
                            section.Append(ch);
                        else
                            stringBuilder.Append(ch);
                    }
                    break;
            }
        }
        Utilities.GetSafeFilename(ref stringBuilder, invalidSubstitute ?? string.Empty, spaceReplacement);
        return stringBuilder.ToString();
    }
    
    private const string DefaultDateTimeFormat = "yyyyMMddHHmm";

    private static readonly Dictionary<char, LevelDataType> LevelDataSubstitutions = new()
    {
        {'B', LevelDataType.BeatsPerMinute },
        {'D', LevelDataType.DifficultyName },
        {'d', LevelDataType.DifficultyShortName },
        {'A', LevelDataType.LevelAuthorName },
        {'a', LevelDataType.SongAuthorName },
        {'I', LevelDataType.LevelId },
        {'J', LevelDataType.NoteJumpSpeed },
        {'L', LevelDataType.SongDurationLabeled },
        {'l', LevelDataType.SongDurationNoLabels },
        {'N', LevelDataType.SongName },
        {'n', LevelDataType.SongSubName },
        {'@', LevelDataType.Date },
        //------CompletionResults----------
        {'1', LevelDataType.FirstPlay },
        {'b', LevelDataType.BadCutsCount },
        {'T', LevelDataType.EndSongTimeLabeled },
        {'t', LevelDataType.EndSongTimeNoLabels },
        {'F', LevelDataType.FullCombo },
        {'M', LevelDataType.Modifiers },
        {'m', LevelDataType.MissedCount },
        {'G', LevelDataType.GoodCutsCount },
        {'E', LevelDataType.LevelEndType },
        {'e', LevelDataType.LevelIncompleteType },
        {'C', LevelDataType.MaxCombo },
        {'S', LevelDataType.RawScore },
        {'s', LevelDataType.ModifiedScore },
        {'R', LevelDataType.Rank },
        {'%', LevelDataType.ScorePercent }
    };

    private static string GetLevelDataString(
        LevelDataType levelDataType,
        ExtendedLevelData levelData,
        ExtendedCompletionResults results,
        string? config = null) => levelDataType switch
    {
        LevelDataType.None => string.Empty,
        LevelDataType.BeatsPerMinute => $"{levelData.BeatsPerMinute:N2}".TrimEnd('0', '.', ','),
        LevelDataType.DifficultyShortName => levelData.Difficulty.FormatShortNameDifficulty(),
        LevelDataType.DifficultyName => levelData.Difficulty.ToString(),
        LevelDataType.LevelAuthorName => int.TryParse(config, out var levelMax) && levelData.LevelAuthorName.Length > levelMax ? levelData.LevelAuthorName.Substring(0, levelMax) : levelData.LevelAuthorName,
        LevelDataType.LevelId => levelData.LevelID,
        LevelDataType.NoteJumpSpeed => $"{levelData.NoteJumpMovementSpeed:N2}".TrimEnd('0', '.', ','),
        LevelDataType.SongAuthorName => int.TryParse(config, out var songMax) && levelData.SongAuthorName.Length > songMax ? levelData.SongAuthorName.Substring(0, songMax) : levelData.SongAuthorName,
        LevelDataType.SongDurationNoLabels => SongDurationNoLabels(TimeSpan.FromSeconds(levelData.SongDuration)),
        LevelDataType.SongDurationLabeled => SongDurationWithLabels(TimeSpan.FromSeconds(levelData.SongDuration)),
        LevelDataType.SongName => int.TryParse(config, out var songNameMax) && levelData.SongName.Length > songNameMax ? levelData.SongName.Substring(0, songNameMax) : levelData.SongName,
        LevelDataType.SongSubName => int.TryParse(config, out var subNameMax) && levelData.SongSubName.Length > subNameMax ? levelData.SongSubName.Substring(0, subNameMax) : levelData.SongSubName,
        LevelDataType.Date => DateTime.Now.ToString(config ?? DefaultDateTimeFormat),
        LevelDataType.FirstPlay => results.PlayCount == 0 ? "1st" : string.Empty,
        LevelDataType.BadCutsCount => results.BadCutsCount.ToString(),
        LevelDataType.EndSongTimeNoLabels => SongDurationNoLabels(TimeSpan.FromSeconds(results.EndSongTime)),
        LevelDataType.EndSongTimeLabeled => SongDurationWithLabels(TimeSpan.FromSeconds(results.EndSongTime)),
        LevelDataType.FullCombo => results.FullCombo ? "FC" : string.Empty,
        LevelDataType.Modifiers => results.FormatModifiers(),
        LevelDataType.GoodCutsCount => results.GoodCutsCount.ToString(),
        LevelDataType.LevelEndType => results.FormatLevelEndState(),
        LevelDataType.LevelIncompleteType => results.FormatLevelIncompleteState(),
        LevelDataType.MaxCombo => results.MaxCombo.ToString(),
        LevelDataType.MissedCount => results.MissedCount.ToString(),
        LevelDataType.ModifiedScore => results.ModifiedScore.ToString(),
        LevelDataType.Rank => results.Rank.ToString(),
        LevelDataType.RawScore => results.RawScore.ToString(),
        LevelDataType.ScorePercent => results.FormatScorePercent(),
        _ => "NA"
    };

    private static string FormatShortNameDifficulty(this BeatmapDifficulty difficulty) => difficulty switch 
    {
        BeatmapDifficulty.Easy => "E",
        BeatmapDifficulty.Normal => "N",
        BeatmapDifficulty.Hard => "H",
        BeatmapDifficulty.Expert => "E",
        BeatmapDifficulty.ExpertPlus => "E+",
        _ => "NA",
    };

    private static string SongDurationNoLabels(TimeSpan time) => $"{time.Minutes}.{time.Seconds:00}";
    private static string SongDurationWithLabels(TimeSpan time) => $"{time.Minutes}m.{time.Seconds:00}s";

    private static string FormatScorePercent(this ExtendedCompletionResults results)
    {
        var str = $"{results.ScorePercent:F3}";
        return str.Substring(0, str.Length - 1);
    }

    private static string FormatLevelEndState(this ExtendedCompletionResults results) => 
        results.LevelEndAction switch
        {
            LevelEndAction.Quit or LevelEndAction.Restart => "Quit",
            _ => results.LevelEndStateType switch
            {
                LevelEndStateType.Incomplete => "Unknown",
                LevelEndStateType.Cleared => "Cleared",
                LevelEndStateType.Failed => "Failed",
                _ => "Unknown",
            }
        };

    private static string FormatLevelIncompleteState(this ExtendedCompletionResults results) => 
        results.LevelEndAction switch
        {
            LevelEndAction.Quit or LevelEndAction.Restart => "Quit",
            _ => results.LevelEndStateType switch
            {
                LevelEndStateType.Incomplete => "Unknown",
                LevelEndStateType.Cleared => string.Empty,
                LevelEndStateType.Failed => "Failed",
                _ => string.Empty,
            }
        };
    
    private static string FormatModifiers(this ExtendedCompletionResults results, string separator = "-")
    {
        var modifiers = results.GameplayModifiers;
        var activeModifiers = new List<string>();
        var energyType = modifiers.energyType switch
        {
            GameplayModifiers.EnergyType.Bar => null,
            GameplayModifiers.EnergyType.Battery => "BE",
            _ => throw new ArgumentOutOfRangeException(nameof(modifiers.energyType))
        };
        var songSpeed = modifiers.songSpeed switch
        {
            GameplayModifiers.SongSpeed.Normal => null,
            GameplayModifiers.SongSpeed.Faster => "FS",
            GameplayModifiers.SongSpeed.Slower => "SS",
            GameplayModifiers.SongSpeed.SuperFast => "SF",
            _ => throw new ArgumentOutOfRangeException(nameof(modifiers.songSpeed))
        };
        var enabledObstacleType = modifiers.enabledObstacleType switch
        {
            GameplayModifiers.EnabledObstacleType.All => null,
            GameplayModifiers.EnabledObstacleType.NoObstacles => "NO",
            GameplayModifiers.EnabledObstacleType.FullHeightOnly => "FHO",
            _ => throw new ArgumentOutOfRangeException(nameof(modifiers.enabledObstacleType))
        };
        if (energyType != null)
            activeModifiers.Add(energyType);
        if (modifiers.noFailOn0Energy && results.Energy.Approximately(0))
            activeModifiers.Add("NF");
        if (modifiers.instaFail)
            activeModifiers.Add("IF");
        if (modifiers.failOnSaberClash)
            activeModifiers.Add("FSC");
        if (enabledObstacleType != null)
            activeModifiers.Add(enabledObstacleType);
        if (modifiers.fastNotes)
            activeModifiers.Add("FN");
        if (modifiers.strictAngles)
            activeModifiers.Add("SA");
        if (modifiers.disappearingArrows)
            activeModifiers.Add("DA");
        if (modifiers.ghostNotes)
            activeModifiers.Add("GN");
        if (modifiers.noBombs)
            activeModifiers.Add("NB");
        if (songSpeed != null)
            activeModifiers.Add(songSpeed);
        if (modifiers.noArrows)
            activeModifiers.Add("NA");
        if (modifiers.proMode)
            activeModifiers.Add("PM");
        if (modifiers.zenMode)
            activeModifiers.Add("ZM");
        if (modifiers.smallCubes)
            activeModifiers.Add("SC");
        return string.Join(separator, activeModifiers);
    }
}