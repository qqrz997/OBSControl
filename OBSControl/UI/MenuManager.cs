using System;
using BeatSaberMarkupLanguage.Settings;
using Zenject;

namespace OBSControl.UI;

internal class MenuManager : IInitializable, IDisposable
{
    private readonly BSMLSettings bsmlSettings;
    private readonly PluginConfig pluginConfig;

    private const string MenuName = "OBSControl";
    private const string ResourcePath = "OBSControl.UI.SettingsView.bsml";
    
    public MenuManager(BSMLSettings bsmlSettings, PluginConfig pluginConfig)
    {
        this.bsmlSettings = bsmlSettings;
        this.pluginConfig = pluginConfig;
    }
    
    public void Initialize()
    {
        bsmlSettings.AddSettingsMenu(MenuName, ResourcePath, pluginConfig);
    }

    public void Dispose()
    {
        bsmlSettings.RemoveSettingsMenu(MenuName);
    }
}