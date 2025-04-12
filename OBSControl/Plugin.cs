using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using IPA.Utilities;
using HarmonyLib;
using UnityEngine.SceneManagement;
using UnityEngine;
using OBSControl.OBSComponents;
using IPALogger = IPA.Logging.Logger;
using BeatSaberMarkupLanguage.Settings;
using BeatSaberMarkupLanguage.Util;
using Object = UnityEngine.Object;

#nullable enable
namespace OBSControl
{
    [Plugin(RuntimeOptions.DynamicInit)]
    public class Plugin
    {
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        internal static Plugin instance;
        internal static PluginConfig config;
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        internal static string Name => "OBSControl";
        internal static bool Enabled;

        [Init]
        public void Init(IPALogger logger, Config conf)
        {
            instance = this;
            Logger.log = logger;
            Logger.log?.Debug("Logger initialized.");
            config = conf.Generated<PluginConfig>();
            OBSWebsocketDotNet.OBSLogger.SetLogger(new OBSLogger());
            MainMenuAwaiter.MainMenuInitializing += OnMenuLoad;
        }

        [OnEnable]
        public void OnEnable()
        {
            //config.Value.FillDefaults();
            Logger.log?.Debug("OnEnable()");
            new GameObject("OBSControl_OBSController").AddComponent<OBSController>();
            new GameObject("OBSControl_RecordingController").AddComponent<RecordingController>();
            ApplyHarmonyPatches();
            Enabled = true;
        }

        [OnDisable]
        public void OnDisable()
        {
            Logger.log?.Debug("OnDisable()");
            RemoveHarmonyPatches();
            Object.Destroy(OBSController.instance?.gameObject);
            Object.Destroy(RecordingController.instance?.gameObject);
            Enabled = false;
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
            BSMLSettings.Instance.AddSettingsMenu("OBSControl", "OBSControl.UI.SettingsView.bsml", config);
        }

        [OnExit]
        public void OnApplicationQuit()
        {
            Logger.log?.Debug("OnApplicationQuit");
            if (RecordingController.instance != null)
                Object.Destroy(RecordingController.instance);
            if (OBSController.instance != null)
                Object.Destroy(OBSController.instance);
        }
    }
}
