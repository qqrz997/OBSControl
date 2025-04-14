using OBSControl.UI;
using Zenject;

namespace OBSControl.Installers;

internal class MenuInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.BindInterfacesTo<MenuManager>().AsSingle();
    }
}