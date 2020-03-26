using ManiaRTRender.Utils;
using OsuRTDataProvider.Listen;
using OsuRTDataProvider.Mods;
using System.Collections.Generic;
using System.Diagnostics;
using static ManiaRTRender.ManiaRTRenderPlugin;

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

        private readonly Judger Judger = new Judger();
        private readonly Stopwatch SW = new Stopwatch();

        public long LastPlayingTime;
        private long LastSystemTime;
        private long TimeSynchronizeInterval = 100;
        public double rate; // for replay rate

        public Game()
        {
            Actions = new List<Action>();
            RawEvents = new List<HitEvent>();
            SpeedRatio = 1.0;
        }

        public bool Start(string beatmap_file, ModsInfo mods_info)
        {
            if (Status != GameStatus.Stop) return true;
            Beatmap = null;
            ManiaBeatmap currentBeatmap = OsuUtils.ReadBeatmap(beatmap_file, mods_info);
            if (currentBeatmap == null)
            {
                return false;
            }
            
            Beatmap = currentBeatmap;
            SpeedRatio = OsuUtils.GetSpeedRatio(mods_info);
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
            LastSystemTime = 0;
            
            rate = 1.0;
            Actions.Clear();
            if (Beatmap != null)
            {
                Judger.init(Beatmap);
            }
            SW.Restart();

            TryToJudge();
        }

        public void SetHitEvents(PlayType play_type, List<HitEvent> hit_events)
        {
            this.PlayType = play_type;
            this.RawEvents = hit_events;

            if (Judger.ShouldReset(hit_events)) // should reset
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
            Judger.TryToJudge(RawEvents, Actions);
        }

        public long GetPlayingTime()
        {
            if (Status != GameStatus.Playing) return LastPlayingTime;
            long interval = SW.ElapsedMilliseconds - LastSystemTime;
            if (interval >= 1.5 * TimeSynchronizeInterval)
            {
                interval = 0;
                Status = GameStatus.Pause;
            }
            long PlayingOffset = PlayType == PlayType.Playing ? TimeSynchronizeInterval : 0;
            return LastPlayingTime + (long)(interval * rate) - PlayingOffset;
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
            long current_time = SW.ElapsedMilliseconds;
            long playing_interval = pt - LastPlayingTime;
            long system_interval = current_time - LastSystemTime;
            rate = (double)playing_interval / system_interval;

            SetTime(pt);
        }

        private void SetTime(long pt)
        {
            LastPlayingTime = pt;
            LastSystemTime = SW.ElapsedMilliseconds;
        }
    }
}
