using System;
using BeatSaberMarkupLanguage.Settings;
using Zenject;

namespace OBSControl.UI;

internal class SettingsMenuManager : IInitializable, IDisposable
{
    private readonly BSMLSettings bsmlSettings;
    private readonly SettingsMenuMain settingsMenuMain;

    private const string MenuName = "OBSControl";
    private const string ResourcePath = "OBSControl.UI.SettingsView.bsml";
    
    public SettingsMenuManager(BSMLSettings bsmlSettings, SettingsMenuMain settingsMenuMain)
    {
        this.bsmlSettings = bsmlSettings;
        this.settingsMenuMain = settingsMenuMain;
    }
    
    public void Initialize()
    {
        bsmlSettings.AddSettingsMenu(MenuName, ResourcePath, settingsMenuMain);
    }

    public void Dispose()
    {
        bsmlSettings.RemoveSettingsMenu(MenuName);
    }
}