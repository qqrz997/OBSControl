using OBSControl.HarmonyPatches;
using Zenject;

namespace OBSControl.Installers;

internal class PlayerInstaller : Installer
{
    private readonly PluginConfig pluginConfig;

    public PlayerInstaller(PluginConfig pluginConfig)
    {
        this.pluginConfig = pluginConfig;
    }

    public override void InstallBindings()
    {
        if (pluginConfig.Enabled)
        {
            Container.BindInterfacesTo<StandardLevelEndHook>().AsSingle();
        }
    }
}