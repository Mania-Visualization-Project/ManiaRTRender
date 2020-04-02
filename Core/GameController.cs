using ManiaRTRender.Render;
using OsuRTDataProvider.Listen;
using OsuRTDataProvider.Mods;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using static ManiaRTRender.ManiaRTRenderPlugin;

namespace ManiaRTRender.Core
{
    class GameController
    {
        private Game game;
        private RenderServer renderServer;

        private PlayType mPlayType = PlayType.Unknown;
        private string mBeatMap = string.Empty;
        private ModsInfo mModsInfo = ModsInfo.Empty;
        private List<HitEvent> mHitEvents = new List<HitEvent>();

        public GameController(int id, OsuRTDataProvider.OsuRTDataProviderPlugin reader)
        {
            OsuListenerManager manager = id < 0 ? reader.ListenerManager : reader.TourneyListenerManagers[id];
            game = new Game();
            renderServer = new RenderServer(game, id);

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
                renderServer.OnPlayerChange(player);
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
                if (game.Start(mBeatMap, mModsInfo))
                {
                    game.SetHitEvents(mPlayType, mHitEvents);
                }
            }
        }
    }
}
