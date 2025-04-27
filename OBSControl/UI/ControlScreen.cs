using System;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using JetBrains.Annotations;
using Zenject;
using static OBSControl.Utilities.PluginResources;

namespace OBSControl.UI;

[ViewDefinition("OBSControl.UI.ControlScreen.bsml")]
internal class ControlScreen : BSMLAutomaticViewController
{
    [Inject] private readonly PluginConfig pluginConfig = null!;

    [UIComponent("window-lock-button")] private readonly ClickableImage windowLockButton = null!;
    
    [Inject] public ControlScreenMainTab ControlScreenMainTab { get; } = null!;
    [Inject] public ControlScreenRecordingTab ControlScreenRecordingTab { get; } = null!;
    [Inject] public ControlScreenStreamingTab ControlScreenStreamingTab { get; } = null!;
    
    public event Action? WindowLockClicked;
    
    [UIAction("#post-parse")]
    public void PostParse()
    {
        windowLockButton.sprite = pluginConfig.ControlScreenLocked ? PinnedIcon : UnpinnedIcon;
    }

    public void ToggleWindowLocked()
    {
        pluginConfig.ControlScreenLocked = !pluginConfig.ControlScreenLocked;
        WindowLockClicked?.Invoke();
        windowLockButton.sprite = pluginConfig.ControlScreenLocked ? PinnedIcon : UnpinnedIcon;
    }
}