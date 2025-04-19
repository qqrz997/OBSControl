using System.Reflection;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using IPA.Loader;
using OBSControl.Installers;
using OBSControl.ObsWebsocket;
using SiraUtil.Zenject;
using Logger = IPA.Logging.Logger;

namespace OBSControl;

[Plugin(RuntimeOptions.DynamicInit)]
internal class Plugin
{
    public static Logger Log { get; private set; } = null!;
    public static Assembly Assembly { get; } = Assembly.GetExecutingAssembly();

    [Init]
    public Plugin(Logger logger, Config config, Zenjector zenjector, PluginMetadata pluginMetadata)
    {
        Log = logger;
        
        OBSWebsocketDotNet.OBSLogger.SetLogger(new ObsLogger());
        
        var pluginConfig = config.Generated<PluginConfig>();
        zenjector.Install<AppInstaller>(Location.App, pluginConfig);
        zenjector.Install<MenuInstaller>(Location.Menu);
        zenjector.Install<PlayerInstaller>(Location.Player);

        Log.Info($"{pluginMetadata.Name} {pluginMetadata.HVersion} initialized.");
    }
}