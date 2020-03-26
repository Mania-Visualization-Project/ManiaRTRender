using ManiaRTRender.Render;
using OsuRTDataProvider;
using OsuRTDataProvider.Listen;
using OsuRTDataProvider.Mods;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace ManiaRTRender.Core
{
    class GameController
    {
        private RenderForm renderForm;
        private Game game;

        private PlayType mPlayType = PlayType.Unknown;
        private string mBeatMap = string.Empty;
        private ModsInfo mModsInfo = ModsInfo.Empty;
        private List<HitEvent> mHitEvents = new List<HitEvent>();

        public GameController(int id, OsuRTDataProviderPlugin reader)
        {
            OsuListenerManager manager = id < 0 ? reader.ListenerManager : reader.TourneyListenerManagers[id];
            game = new Game();
            new Thread(() =>
            {
                renderForm = new RenderForm(game, id);
                Application.Run(renderForm);
            }).Start();

            manager.OnHitEventsChanged += (playType, hitEvents) =>
            {
                mPlayType = playType;
                mHitEvents = hitEvents;
                Process();
            };

            manager.OnPlayingTimeChanged += (ms) =>
            {
                game.SynchronizeTime(ms);
            };

            manager.OnModsChanged += (mods) =>
            {
                mModsInfo = mods;
                Process();
            };

            manager.OnBeatmapChanged += (beatmap) =>
            {
                mBeatMap = beatmap.FilenameFull;
                Process();
            };

            manager.OnPlayerChanged += (string player) =>
            {
                renderForm.SetPlayer(player);
            };
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
    }
}
