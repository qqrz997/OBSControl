using System;
using BeatSaberMarkupLanguage.MenuButtons;
using Zenject;

namespace OBSControl.UI;

internal class MenuButtonManager : IInitializable, IDisposable
{
    private readonly PluginConfig pluginConfig;
    private readonly ControlScreenManager controlScreenManager;
    private readonly MenuButtons menuButtons;

    private readonly MenuButton showButton;
    private readonly MenuButton hideButton;
    
    public MenuButtonManager(
        PluginConfig pluginConfig,
        ControlScreenManager controlScreenManager,
        MenuButtons menuButtons)
    {
        this.pluginConfig = pluginConfig;
        this.controlScreenManager = controlScreenManager;
        this.menuButtons = menuButtons;

        showButton = new("<color=#e36464>OBS Controls</color>", "Show OBS Control Screen", ShowButtonClicked);
        hideButton = new("<color=#88e364>OBS Controls</color>", "Hide OBS Control Screen", HideButtonClicked);
    }

    public void Initialize() => RegisterCurrentButton();
    public void Dispose() => UnregisterCurrentButton();

    private void ShowButtonClicked()
    {
        pluginConfig.ShowControlScreen = true;
        controlScreenManager.UpdateVisibility();
        UnregisterCurrentButton();
        RegisterCurrentButton();
    }

    private void HideButtonClicked()
    {
        pluginConfig.ShowControlScreen = false;
        controlScreenManager.UpdateVisibility();
        UnregisterCurrentButton();
        RegisterCurrentButton();
    }

    private void RegisterCurrentButton()
    {
        menuButtons.RegisterButton(pluginConfig.ShowControlScreen ? hideButton : showButton);
    }

    private void UnregisterCurrentButton()
    {
        menuButtons.UnregisterButton(pluginConfig.ShowControlScreen ? showButton : hideButton);
    }
}