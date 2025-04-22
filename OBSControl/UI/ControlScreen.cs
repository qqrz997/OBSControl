using System.Collections.Generic;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using JetBrains.Annotations;
using OBSControl.Managers;
using OBSControl.UI.Formatters;
using UnityEngine;
using Zenject;

namespace OBSControl.UI;

[ViewDefinition("OBSControl.UI.ControlScreen.bsml")]
internal class ControlScreen : BSMLAutomaticViewController
{
    [Inject, UsedImplicitly]
    public ControlScreenMainTab ControlScreenMainTab { get; } = null!;

    [Inject, UsedImplicitly]
    public ControlScreenRecordingTab ControlScreenRecordingTab { get; } = null!;

    [Inject, UsedImplicitly]
    public ControlScreenStreamingTab ControlScreenStreamingTab { get; } = null!;

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
    
    [UIAction("#post-parse")]
    public void PostParse()
    {
    }
}