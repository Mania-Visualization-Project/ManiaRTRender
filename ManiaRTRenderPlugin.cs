using IpcLibrary;
using ManiaRTRender.Core;
using ManiaRTRender.Utils;
using Sync;
using Sync.Plugins;
using Sync.Tools;
using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Channels.Ipc;

namespace ManiaRTRender
{
    [SyncPluginDependency("7216787b-507b-4eef-96fb-e993722acf2e", Version = "^1.6.1", Require = true)]
    [SyncPluginID("f4cb1b67-a036-41ad-b596-dee5490d4637", VERSION)]
    public class ManiaRTRenderPlugin : Plugin
    {
        private readonly List<GameController> _gameControllers = new List<GameController>();
        public const string PLUGIN_NAME = "ManiaRTRender";
        public const string PLUGIN_AUTHOR = "Kuit";
        public const string VERSION = "1.1.6";

        public ManiaRTRenderPlugin() : base(PLUGIN_NAME, PLUGIN_AUTHOR)
        {

            //IpcUtils.RegisterObj(IpcConstants.OBJECT_CONFIG_NAME, RemoteConfig.INSTANCE);
            try
            {
                RemoteConfig.id = SerializeUtils.InitShareMemory(IpcConstants.OBJECT_CONFIG_NAME, IpcConstants.SIZE_CONFIG);
                var buff = new byte[4096];
                var length = new RemoteConfig().Write(ref buff, 0);
                SerializeUtils.Save(RemoteConfig.id, ref buff, length);
            } catch (Exception e)
            {
                Logger.E(e.StackTrace);
            }
            

            new PluginConfigurationManager(this).AddItem(new SettingIni());
            EventBus.BindEvent<PluginEvents.LoadCompleteEvent>(OnAllPluginLoadedFinish);
        }

        public override void OnEnable()
        {
            IO.CurrentIO.WriteColor(getName() + " By " + getAuthor(), ConsoleColor.DarkCyan);
        }

        private void OnAllPluginLoadedFinish(PluginEvents.LoadCompleteEvent loadCompleteEvent)
        {
            var host = loadCompleteEvent.Host;

            foreach (var plugin in host.EnumPluings())
            {
                if (plugin.Name != "OsuRTDataProvider") continue;
                var reader = plugin as OsuRTDataProvider.OsuRTDataProviderPlugin;

                // fetch ListenInterval from ORTDP ini because there is no API.
                new PluginConfigurationManager(reader).AddItem(new ORTDPSetting.SettingIni());

                if (reader != null && reader.TourneyListenerManagersCount == 0)
                {
                    _gameControllers.Add(new GameController(-1, reader));
                    //GameControllers.Add(new GameController(1, reader));
                }
                else
                {
                    if (reader == null) continue;
                    for (var i = 0; i < reader.TourneyListenerManagersCount; i++)
                    {
                        _gameControllers.Add(new GameController(i, reader));
                    }
                }
            }
        }

        public static class Logger
        {
            private static readonly Logger<ManiaRTRenderPlugin> logger = new Logger<ManiaRTRenderPlugin>();
            public static void I(string message) => logger.LogInfomation(message);
            public static void E(string message) => logger.LogError(message);
            public static void W(string message) => logger.LogWarning(message);
        }
    }
}
