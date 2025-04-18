﻿using System.Collections.Generic;

namespace OBSControl.Models;

public interface IGameplayModifiers
{
    EnergyType EnergyType { get; }
    bool BatteryEnergy { get; }
    bool NoFail { get; }
    bool DemoNoFail { get; }
    bool InstaFail { get; }
    bool FailOnSaberClash { get; }
    EnabledObstacleType EnabledObstacleType { get; }
    bool NoObstacles { get; }
    bool DemoNoObstacles { get; }
    bool FastNotes { get; }
    bool StrictAngles { get; }
    bool DisappearingArrows { get; }
    bool GhostNotes { get; }
    bool NoBombs { get; }
    SongSpeed SongSpeed { get; }
    float SongSpeedMul { get; }
    bool NoArrows { get; }
}

public enum EnabledObstacleType
{
    All,
    FullHeightOnly,
    NoObstacles
}

	
public enum EnergyType
{
    Bar,
    Battery
}

	
public enum SongSpeed
{
    Normal,
    Faster,
    Slower
}

public static class ModifiersExtensions
{


    public static string ToModifierString(this IGameplayModifiers? modifiers, string? separator = "_")
    {
        if (modifiers == null)
            return string.Empty;
        if (separator == null)
            separator = string.Empty;
        List<string> activeModifiers = new List<string>();
        if (modifiers.SongSpeed != SongSpeed.Normal)
        {
            if (modifiers.SongSpeed == SongSpeed.Faster)
                activeModifiers.Add("FS");
            else
                activeModifiers.Add("SS");
        }
        if (modifiers.DisappearingArrows)
            activeModifiers.Add("DA");
        if (modifiers.GhostNotes)
            activeModifiers.Add("GN");
        if (modifiers.BatteryEnergy)
            activeModifiers.Add("BE");
        if (modifiers.DemoNoFail)
            activeModifiers.Add("DNF");
        if (modifiers.DemoNoObstacles)
            activeModifiers.Add("DNO");
        if (modifiers.EnabledObstacleType != EnabledObstacleType.All)
        {
            if (modifiers.EnabledObstacleType == EnabledObstacleType.FullHeightOnly)
                activeModifiers.Add("FHO");
            else // No obstacles
                activeModifiers.Add("NO");
        }
        //if (modifiers.energyType == GameplayModifiers.EnergyType.Battery)
        //    activeModifiers.Add("BE");
        if (modifiers.FailOnSaberClash)
            activeModifiers.Add("FSC");
        if (modifiers.FastNotes)
            activeModifiers.Add("FN");
        if (modifiers.InstaFail)
            activeModifiers.Add("IF");
        if (modifiers.NoArrows)
            activeModifiers.Add("NA");
        if (modifiers.NoBombs)
            activeModifiers.Add("NB");
        if (modifiers.NoFail)
            activeModifiers.Add("NF");
        if (modifiers.StrictAngles)
            activeModifiers.Add("SA");
        return string.Join(separator, activeModifiers);
    }
}