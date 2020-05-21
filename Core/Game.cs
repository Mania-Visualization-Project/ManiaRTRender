using ManiaRTRender.Utils;
using OsuRTDataProvider.Listen;
using OsuRTDataProvider.Mods;
using System.Collections.Generic;
using System.Diagnostics;

namespace ManiaRTRender.Core
{
    public enum GameStatus
    {
        Stop = 0,
        Playing = 1,
        Pause = 2
    }

    public class Game
    {
        public List<Action> Actions { get; private set; }
        public List<HitEvent> RawEvents { get; private set; }
        public ManiaBeatmap Beatmap { get; private set; }
        public double SpeedRatio { get; private set; }
        public PlayType PlayType { get; private set; }
        public GameStatus Status { get; private set; }

        private readonly Judger _judger = new Judger();
        private readonly Stopwatch _sw = new Stopwatch();

        public long LastPlayingTime;
        private long _lastSystemTime;
        public double Rate; // for replay rate

        public Game()
        {
            Actions = new List<Action>();
            RawEvents = new List<HitEvent>();
            SpeedRatio = 1.0;
        }

        public bool Start(string beatmapFile, ModsInfo modsInfo)
        {
            if (Status != GameStatus.Stop) return true;
            Beatmap = null;
            var currentBeatmap = OsuUtils.ReadBeatmap(beatmapFile, modsInfo);
            if (currentBeatmap == null)
            {
                return false;
            }
            
            Beatmap = currentBeatmap;
            SpeedRatio = OsuUtils.GetSpeedRatio(modsInfo);
            PlayType = PlayType.Unknown;
            Status = GameStatus.Playing;

            Reset();
            
            return true;
        }

        public void Stop()
        {
            Status = GameStatus.Stop;
            Reset();
        }

        private void Reset()
        {
            LastPlayingTime = long.MaxValue;
            _lastSystemTime = 0;
            
            Rate = 1.0;
            lock (Actions)
            {
                Actions.Clear();
            }
            if (Beatmap != null)
            {
                _judger.Init(Beatmap);
            }
            _sw.Restart();

            TryToJudge();
        }

        public void SetHitEvents(PlayType playType, List<HitEvent> hitEvents)
        {
            this.PlayType = playType;
            this.RawEvents = hitEvents;

            if (_judger.ShouldReset(hitEvents)) // should reset
            {
                Reset();
            }
            else
            {
                TryToJudge();
            }
        }

        private void TryToJudge()
        {
            _judger.TryToJudge(RawEvents, Actions);
        }

        public long GetPlayingTime()
        {
            if (Status != GameStatus.Playing) return LastPlayingTime;
            long interval = _sw.ElapsedMilliseconds - _lastSystemTime;
            if (interval >= 1.5 * Setting.ORTDPListenInterval)
            {
                // wait for too long time, pause!
                interval = 0;
                Status = GameStatus.Pause;
            }
            long playingOffset = PlayType == PlayType.Playing ? Setting.ORTDPListenInterval : 0;
            return LastPlayingTime + (long)(interval * Rate) - playingOffset;
        }

        public void SynchronizeTime(long pt)
        {
            if (Status == GameStatus.Stop) return;
            if (pt < LastPlayingTime || Status != GameStatus.Playing)
            {
                Status = GameStatus.Playing;
                Reset();
                SetTime(pt);
                return;
            }
            Status = GameStatus.Playing;

            // playing -> playing : calculate rate
            var currentTime = _sw.ElapsedMilliseconds;
            var playingInterval = pt - LastPlayingTime;
            var systemInterval = currentTime - _lastSystemTime;
            Rate = (double)playingInterval / systemInterval;

            SetTime(pt);
        }

        private void SetTime(long pt)
        {
            LastPlayingTime = pt;
            _lastSystemTime = _sw.ElapsedMilliseconds;
        }
    }
}
