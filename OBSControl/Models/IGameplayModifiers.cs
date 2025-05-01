using System;
using System.Collections.Generic;

namespace OBSControl.Models;

public static class ModifiersExtensions
{
    public static string ToModifierString(this GameplayModifiers modifiers, string separator = "_")
    {
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
        // todo - check for 0 energy
        if (modifiers.noFailOn0Energy)
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