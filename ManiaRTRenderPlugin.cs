using ManiaRTRender.Core;
using Sync;
using Sync.Plugins;
using Sync.Tools;
using System;
using System.Collections.Generic;

namespace ManiaRTRender
{
    [SyncPluginDependency("7216787b-507b-4eef-96fb-e993722acf2e", Version = "^1.6.1", Require = true)]
    [SyncPluginID("f4cb1b67-a036-41ad-b596-dee5490d4637", VERSION)]
    public class ManiaRTRenderPlugin : Plugin
    {
        private List<GameController> GameControllers = new List<GameController>();
        public const string PLUGIN_NAME = "ManiaRTRender";
        public const string PLUGIN_AUTHOR = "Kuit";
        public const string VERSION = "1.0.0";

        public ManiaRTRenderPlugin() : base(PLUGIN_NAME, PLUGIN_AUTHOR)
        {
            new PluginConfigurationManager(this).AddItem(new SettingIni());
            EventBus.BindEvent<PluginEvents.LoadCompleteEvent>(OnAllPluginLoadedFinish);
        }

        public override void OnEnable()
        {
            Sync.Tools.IO.CurrentIO.WriteColor(getName() + " By " + getAuthor(), ConsoleColor.DarkCyan);
        }

        private void OnAllPluginLoadedFinish(PluginEvents.LoadCompleteEvent loadCompleteEvent)
        {

            SyncHost host = loadCompleteEvent.Host;

            foreach (var plugin in host.EnumPluings())
            {
                if (plugin.Name == "OsuRTDataProvider")
                {
                    OsuRTDataProvider.OsuRTDataProviderPlugin reader = plugin as OsuRTDataProvider.OsuRTDataProviderPlugin;

                    if (reader.TourneyListenerManagersCount == 0)
                    {
                        GameControllers.Add(new GameController(-1, reader));
                    }
                    else
                    {
                        for (int i = 0; i < reader.TourneyListenerManagersCount; i++)
                        {
                            GameControllers.Add(new GameController(i, reader));
                        }
                    }

                    // fetch ListenInterval from ORTDP ini because there is no API.
                    new PluginConfigurationManager(reader).AddItem(new ORTDPSetting.SettingIni());
                }
            }
        }

        public static class Logger
        {
            static Logger<ManiaRTRenderPlugin> logger = new Logger<ManiaRTRenderPlugin>();
            public static void I(string message) => logger.LogInfomation(message);
            public static void E(string message) => logger.LogError(message);
            public static void W(string message) => logger.LogWarning(message);
        }
    }
}
