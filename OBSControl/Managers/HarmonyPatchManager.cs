using System;
using HarmonyLib;
using Zenject;

namespace OBSControl.Managers;

public class HarmonyPatchManager : IInitializable, IDisposable
{
    private readonly Harmony harmony = new("com.github.Zingabopp.OBSControl");

    public void Initialize()
    {
        harmony.PatchAll(Plugin.Assembly);
    }

    public void Dispose()
    {
        harmony.UnpatchSelf();
    }
}