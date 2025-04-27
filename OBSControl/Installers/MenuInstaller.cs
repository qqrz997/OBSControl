using OBSControl.UI;
using OBSControl.UI.Formatters;
using Zenject;

namespace OBSControl.Installers;

internal class MenuInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.BindInterfacesTo<MenuButtonManager>().AsSingle();
        Container.BindInterfacesTo<SettingsMenuManager>().AsSingle();
        Container.BindInterfacesAndSelfTo<ControlScreenManager>().AsSingle();
        
        Container.BindInterfacesAndSelfTo<PluginSettingsView>().AsSingle();
        Container.Bind<AudioDeviceSettingsView>().AsSingle();
        Container.Bind<RecordingSettingsView>().AsSingle();
        Container.Bind<SceneSettingsView>().AsSingle();
        
        Container.Bind<ControlScreen>().FromNewComponentAsViewController().AsSingle();
        Container.BindInterfacesAndSelfTo<ControlScreenMainTab>().AsSingle();
        Container.BindInterfacesAndSelfTo<ControlScreenRecordingTab>().AsSingle();
        Container.BindInterfacesAndSelfTo<ControlScreenStreamingTab>().AsSingle();

        Container.Bind<BoolFormatter>().AsSingle();
        Container.Bind<TimeFormatter>().AsSingle();
    }
}