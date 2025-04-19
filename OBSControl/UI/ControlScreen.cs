using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using JetBrains.Annotations;
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
    
    public bool WindowLocked { get; set; } = false;
    public bool WindowUnlocked => !WindowLocked;
}