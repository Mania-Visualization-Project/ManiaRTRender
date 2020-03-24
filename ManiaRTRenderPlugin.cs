using ManiaRTRender.Core;
using ManiaRTRender.Render;
using OsuRTDataProvider.Listen;
using OsuRTDataProvider.Mods;
using Sync;
using Sync.Plugins;
using Sync.Tools;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace ManiaRTRender
{
    //[SyncPluginDependency("7216787b-507b-4eef-96fb-e993722acf2e", Require = true)]
    public class ManiaRTRenderPlugin : Plugin
    {
        public static Logger<ManiaRTRenderPlugin> logger = new Logger<ManiaRTRenderPlugin>();

        private RenderForm renderForm;
        private Game game;

        private PlayType mPlayType = PlayType.Unknown;
        private string mBeatMap = string.Empty;
        private ModsInfo mModsInfo = ModsInfo.Empty;
        private List<HitEvent> mHitEvents = new List<HitEvent>();

        public ManiaRTRenderPlugin() : base("ManiaRTRender", "Kuit")
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
            game = new Game();
            new Thread(() =>
            {
                renderForm = new RenderForm(game);
                Application.Run(renderForm);
            }).Start();

            SyncHost host = loadCompleteEvent.Host;

            foreach (var plugin in host.EnumPluings())
            {
                if (plugin.Name == "OsuRTDataProvider")
                {
                    OsuRTDataProvider.OsuRTDataProviderPlugin reader = plugin as OsuRTDataProvider.OsuRTDataProviderPlugin;

                    reader.ListenerManager.OnHitEventsChanged += (playType, hitEvents) =>
                    {
                        mPlayType = playType;
                        mHitEvents = hitEvents;
                        Process();
                    };

                    reader.ListenerManager.OnPlayingTimeChanged += (ms) =>
                    {
                        game.SynchronizeTime(ms);
                    };

                    reader.ListenerManager.OnModsChanged += (mods) =>
                    {
                        mModsInfo = mods;
                        Process();
                    };

                    reader.ListenerManager.OnBeatmapChanged += (beatmap) =>
                    {
                        mBeatMap = beatmap.FilenameFull;
                        Process();
                    };
                }
            }
        }

        private void Process()
        {
            if (mPlayType == PlayType.Unknown)
            {
                game.Stop();
                return;
            }
            if (mModsInfo != ModsInfo.Empty && mBeatMap != string.Empty)
            {
                game.Start(mBeatMap, mModsInfo);
                game.SetHitEvents(mPlayType, mHitEvents);
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
