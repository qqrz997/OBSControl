using OBSControl.HarmonyPatches;
using OBSControl.Managers;
using Zenject;

namespace OBSControl.Installers;

internal class AppInstaller : Installer
{
    private readonly PluginConfig pluginConfig;

    public AppInstaller(PluginConfig pluginConfig)
    {
        this.pluginConfig = pluginConfig;
    }

    public override void InstallBindings()
    {
        Container.BindInstance(pluginConfig).AsSingle();
        
        Container.BindInterfacesAndSelfTo<RecordingManager>().AsSingle();
        Container.BindInterfacesAndSelfTo<StreamingManager>().AsSingle();
        Container.BindInterfacesAndSelfTo<ObsManager>().AsSingle();

        Container.BindInterfacesTo<HarmonyPatchManager>().AsSingle();
        Container.BindInterfacesTo<StartLevelButtonHook>().AsSingle();
    }
}