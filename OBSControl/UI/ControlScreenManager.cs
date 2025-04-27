using System;
using BeatSaberMarkupLanguage.FloatingScreen;
using HMUI;
using UnityEngine;
using Zenject;
using static BeatSaberMarkupLanguage.FloatingScreen.FloatingScreen;

namespace OBSControl.UI;

internal class ControlScreenManager : IInitializable, IDisposable
{
    private readonly PluginConfig pluginConfig;
    private readonly ControlScreen controlScreen;
    private readonly FloatingScreen floatingScreen;

    public ControlScreenManager(PluginConfig pluginConfig, ControlScreen controlScreen)
    {
        this.pluginConfig = pluginConfig;
        this.controlScreen = controlScreen;
        floatingScreen = CreateFloatingScreen(new(100f, 50f), true, Vector3.zero, Quaternion.identity);
    }

    public void Initialize()
    {
        controlScreen.WindowLockClicked += ControlScreenWindowLockClicked;
        floatingScreen.HandleReleased += ControlScreenHandleReleased;
        
        floatingScreen.SetRootViewController(controlScreen, ViewController.AnimationType.None);
        floatingScreen.transform.position = pluginConfig.ControlScreenPosition;
        floatingScreen.transform.rotation = pluginConfig.ControlScreenRotation;
        
        floatingScreen.ShowHandle = !pluginConfig.ControlScreenLocked;
        
        UpdateVisibility();
    }

    public void Dispose()
    {
        controlScreen.WindowLockClicked -= ControlScreenWindowLockClicked;
        if (floatingScreen == null) return;
        floatingScreen.HandleReleased -= ControlScreenHandleReleased;
    }

    public void UpdateVisibility()
    {
        if (floatingScreen == null) return;
        floatingScreen.gameObject.SetActive(pluginConfig.ShowControlScreen);
    }

    private void UpdateMovable()
    {
        if (floatingScreen == null) return;
        floatingScreen.ShowHandle = !pluginConfig.ControlScreenLocked;
    }

    private void ControlScreenWindowLockClicked()
    {
        UpdateMovable();
    }

    private void ControlScreenHandleReleased(object sender, FloatingScreenHandleEventArgs eventArgs)
    {
        pluginConfig.ControlScreenPosition = eventArgs.Position;
        pluginConfig.ControlScreenRotation = eventArgs.Rotation;
    }
}