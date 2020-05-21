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
        private readonly Game _game;
        private readonly RenderServer renderServer;

        private PlayType _mPlayType = PlayType.Unknown;
        private string _mBeatMap = string.Empty;
        private ModsInfo _mModsInfo = ModsInfo.Empty;
        private List<HitEvent> _mHitEvents = new List<HitEvent>();

        public GameController(int id, OsuRTDataProvider.OsuRTDataProviderPlugin reader)
        {
            var manager = id < 0 ? reader.ListenerManager : reader.TourneyListenerManagers[id];
            if (id < 0) id = 0;
            _game = new Game();
            renderServer = new RenderServer(_game, id);

            manager.OnHitEventsChanged += (playType, hitEvents) =>
            {
                _mPlayType = playType;
                _mHitEvents = hitEvents;
                Process();
            };

            manager.OnPlayingTimeChanged += (ms) =>
            {
                _game.SynchronizeTime(ms);
            };

            manager.OnModsChanged += (mods) =>
            {
                _mModsInfo = mods;
                Process();
            };

            manager.OnBeatmapChanged += (beatmap) =>
            {
                _mBeatMap = beatmap.FilenameFull;
                Process();
            };

            manager.OnPlayerChanged += (string player) =>
            {
                renderServer.OnPlayerChange(player);
            };
        }

        private void Process()
        {
            if (_mPlayType == PlayType.Unknown)
            {
                _game.Stop();
                return;
            }
            if (_mModsInfo != ModsInfo.Empty && _mBeatMap != string.Empty)
            {
                if (_game.Start(_mBeatMap, _mModsInfo))
                {
                    _game.SetHitEvents(_mPlayType, _mHitEvents);
                }
            }
        }
    }
}
