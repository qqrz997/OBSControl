using System;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using JetBrains.Annotations;
using Zenject;

namespace OBSControl.UI;

[ViewDefinition("OBSControl.UI.ControlScreen.bsml")]
internal class ControlScreen : BSMLAutomaticViewController
{
    [Inject] private readonly PluginConfig pluginConfig = null!;
    
    [Inject, UsedImplicitly]
    public ControlScreenMainTab ControlScreenMainTab { get; } = null!;

    [Inject, UsedImplicitly]
    public ControlScreenRecordingTab ControlScreenRecordingTab { get; } = null!;

    [Inject, UsedImplicitly]
    public ControlScreenStreamingTab ControlScreenStreamingTab { get; } = null!;

    [UIAction("#post-parse")]
    public void PostParse()
    {
        WindowLocked = pluginConfig.ControlScreenLocked;
    }
    
    public event Action<bool>? WindowLockClicked;
    
    private bool windowLocked;
    public bool WindowLocked
    {
        get => windowLocked;
        set
        {
            windowLocked = value;
            NotifyPropertyChanged();
            NotifyPropertyChanged(nameof(WindowUnlocked));
        }
    }
    public bool WindowUnlocked => !WindowLocked;

    public void LockWindow()
    {
        WindowLocked = true;
        WindowLockClicked?.Invoke(true);
    }

    public void UnlockWindow()
    {
        WindowLocked = false;
        WindowLockClicked?.Invoke(false);
    }
}