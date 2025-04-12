﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace OBSControl.HarmonyPatches;

public static class HarmonyManager
{
    public const string HarmonyId = "com.github.Zingabopp.OBSControl";

    private static Harmony? _harmony;
    private static readonly BindingFlags allBindingFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static HarmonyPatchInfo? LevelDelayPatch;
        
    internal static Harmony Harmony => _harmony ??= new Harmony(HarmonyId);
        
    internal static readonly HashSet<HarmonyPatchInfo> AppliedPatches = new HashSet<HarmonyPatchInfo>();

    public static bool ApplyPatch(HarmonyPatchInfo patchInfo)
    {
        return patchInfo.ApplyPatch(Harmony);
    }

    public static bool RemovePatch(HarmonyPatchInfo patchInfo)
    {
        return patchInfo.RemovePatch(Harmony);
    }

    public static bool ApplyPatch(Harmony harmony, MethodInfo original, HarmonyMethod? prefix = null, HarmonyMethod? postfix = null)
    {
        try
        {
            string? patchTypeName = null;
            if (prefix != null)
                patchTypeName = prefix.method.DeclaringType?.Name;
            else if (postfix != null)
                patchTypeName = postfix.method.DeclaringType?.Name;
            Logger.log?.Debug($"Harmony patching {original.Name} with {patchTypeName}");
            harmony.Patch(original, prefix, postfix);
            return true;
        }
        catch (Exception e)
        {
            Logger.log?.Error($"Unable to patch method {original.Name}: {e.Message}");
            Logger.log?.Debug(e);
            return false;
        }
    }

    public static void ApplyDefaultPatches()
    {

    }

    public static void UnpatchAll()
    {
        foreach (var patch in AppliedPatches.ToList())
        {
            patch.RemovePatch();
        }
        Harmony.UnpatchSelf();
    }
    public static HarmonyPatchInfo GetLevelDelayPatch()
    {
        if(LevelDelayPatch == null)
        {
            MethodInfo original = typeof(SinglePlayerLevelSelectionFlowCoordinator).GetMethod("StartLevel", allBindingFlags);
            HarmonyMethod prefix = new HarmonyMethod(typeof(LevelSelectionNavigationController_StartLevel).GetMethod("Prefix", allBindingFlags));
            LevelDelayPatch = new HarmonyPatchInfo(Harmony, original, prefix, null);
        }
        return LevelDelayPatch;
    }

}