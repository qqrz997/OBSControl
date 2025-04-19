using System;
using BeatSaberMarkupLanguage.Settings;
using Zenject;

namespace OBSControl.UI;

internal class SettingsMenuManager : IInitializable, IDisposable
{
    private readonly BSMLSettings bsmlSettings;
    private readonly PluginSettingsView pluginSettingsView;

    private const string MenuName = "OBSControl";
    private const string ResourcePath = "OBSControl.UI.SettingsView.bsml";
    
    public SettingsMenuManager(BSMLSettings bsmlSettings, PluginSettingsView pluginSettingsView)
    {
        this.bsmlSettings = bsmlSettings;
        this.pluginSettingsView = pluginSettingsView;
    }
    
    public void Initialize()
    {
        bsmlSettings.AddSettingsMenu(MenuName, ResourcePath, pluginSettingsView);
    }

    public void Dispose()
    {
        bsmlSettings.RemoveSettingsMenu(MenuName);
    }
}