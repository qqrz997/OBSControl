using BeatSaberMarkupLanguage.Settings;
using BeatSaberMarkupLanguage.Util;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using IPA.Loader;
using OBSControl.OBSComponents;
using UnityEngine;
using Logger = IPA.Logging.Logger;

namespace OBSControl;

[Plugin(RuntimeOptions.DynamicInit)]
internal class Plugin
{
    public static PluginConfig Config { get; private set; } = null!;
    public static Logger Log { get; private set; } = null!;

    [Init]
    public Plugin(Logger logger, Config config, PluginMetadata pluginMetadata)
    {
        Log = logger;
        Config = config.Generated<PluginConfig>();
        
        OBSWebsocketDotNet.OBSLogger.SetLogger(new OBSLogger());
        MainMenuAwaiter.MainMenuInitializing += OnMenuLoad;

        Log.Info($"{pluginMetadata.Name} {pluginMetadata.HVersion} initialized.");
    }

    [OnEnable]
    public void OnEnable()
    {
        new GameObject("OBSControl_OBSController").AddComponent<OBSController>();
        new GameObject("OBSControl_RecordingController").AddComponent<RecordingController>();
        ApplyHarmonyPatches();
    }

    [OnDisable]
    public void OnDisable()
    {
        RemoveHarmonyPatches();
        if (RecordingController.instance != null)
            Object.Destroy(RecordingController.instance);
        if (OBSController.instance != null)
            Object.Destroy(OBSController.instance);
    }

    [OnExit]
    public void OnApplicationQuit()
    {
        if (RecordingController.instance != null)
            Object.Destroy(RecordingController.instance);
        if (OBSController.instance != null)
            Object.Destroy(OBSController.instance);
    }

    private static void ApplyHarmonyPatches()
    {
        HarmonyPatches.HarmonyManager.ApplyDefaultPatches();
    }

    private static void RemoveHarmonyPatches()
    {
        // Removes all patches with this HarmonyId
        HarmonyPatches.HarmonyManager.UnpatchAll();
    }

    private static void OnMenuLoad()
    {
        BSMLSettings.Instance.AddSettingsMenu("OBSControl", "OBSControl.UI.SettingsView.bsml", Config);
    }
}