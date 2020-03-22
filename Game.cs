using OsuRTDataProvider.Listen;
using OsuRTDataProvider.Mods;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ManiaRTRender.ManiaRTRenderPlugin;

namespace ManiaRTRender
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

        private Dictionary<int, Action> CurrentHolding;
        private LinkedList<Note> NotesToJudgement;
        public GameStatus Status { get; private set; }
        private int ActionIndex = 0;

        public long LastPlayingTime;
        private long LastSystemTime;
        private long TimeSynchronizeInterval = 100;
        private readonly Stopwatch SW = new Stopwatch();
        public double rate; // for replay rate

        public Game()
        {
            Actions = new List<Action>();
            CurrentHolding = new Dictionary<int, Action>();
            NotesToJudgement = new LinkedList<Note>();
            RawEvents = new List<HitEvent>();
            SpeedRatio = 1.0;
        }

        public void Start(string beatmap_file, ModsInfo mods_info)
        {
            if (Status != GameStatus.Stop) return;
            Beatmap = null;
            Logger.I($"start: {beatmap_file} ({mods_info})");
            ManiaBeatmap currentBeatmap = OsuUtils.ReadBeatmap(beatmap_file, mods_info);
            if (currentBeatmap == null)
            {
                return;
            }
            

            Logger.E($"reset due to start");
            Beatmap = currentBeatmap;

            Reset();

            SpeedRatio = OsuUtils.GetSpeedRatio(mods_info);
            PlayType = PlayType.Unknown;

            Status = GameStatus.Playing;
        }

        public void Stop()
        {
            Status = GameStatus.Stop;
            Logger.E("reset due to stop");

            Reset();
        }

        private void Reset()
        {
            LastPlayingTime = long.MaxValue;
            LastSystemTime = 0;
            ActionIndex = 0;
            rate = 1.0;
            Actions.Clear();
            CurrentHolding.Clear();
            SW.Restart();
            NotesToJudgement.CopyFrom(Beatmap.Notes);

            TryToJudge();
        }

        private Judgement GetJudgement(double diff)
        {
            for (int i = 0; i < Beatmap.JudgementWindow.Length; i++)
            {
                if (diff <= Beatmap.JudgementWindow[i])
                {
                    return (Judgement) i;
                }
            }
            return Judgement.MISS;
        }

        private Note FindTarget(Action action)
        {
            LinkedListNode<Note> list_node = NotesToJudgement.First;
            while (list_node != null)
            {
                Note note = list_node.Value;
                if (note.Column == action.Column)
                {
                    long diff = note.TimeStamp - action.TimeStamp;
                    if (Math.Abs(diff) > Beatmap.JudgementWindow.Last())
                    {
                        if (diff > 0) break;
                    }
                    else
                    {
                        NotesToJudgement.Remove(list_node);
                        return note;
                    }
                }
                list_node = list_node.Next;
            }

            return null;
        }

        private bool JudgeHold(Action action)
        {
            Note target = FindTarget(action);
            if (target == null)
            {
                // user hits nothing
                return false;
            }

            Judgement judgement = GetJudgement(Math.Abs(action.TimeStamp - target.TimeStamp));
            action.JudgementStart = judgement;
            target.Judgement = judgement;
            action.Target = target;
            return target.Duration != 0;
        }

        private void JudgeRelease(Action action)
        {
            if (action.Target == null)
            {
                Logger.E("Action.Target == null!!!");
                return;
            }
            long diff = Math.Abs(action.EndTime - action.Target.EndTime);
            diff = (long)(diff / 1.5); // LN lenience
            action.JudgementEnd = GetJudgement(diff);

            // adjust target's judgement
            long start_diff = Math.Abs(action.TimeStamp - action.Target.TimeStamp);
            diff = (diff + start_diff) / 2;
            action.Target.Judgement = GetJudgement(diff);
        }

        

        public void SetHitEvents(PlayType play_type, List<HitEvent> hit_events)
        {
            this.PlayType = play_type;
            this.RawEvents = hit_events;

            if (ActionIndex > hit_events.Count)
            {
                Reset();
            }
            else
            {
                TryToJudge();
            }
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
            return LastPlayingTime + (long)(interval * rate);
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
            Logger.E($"pt: {pt}");
        }

        private void SetTime(long pt)
        {
            LastPlayingTime = pt;
            LastSystemTime = SW.ElapsedMilliseconds;
        }

        private void TryToJudge()
        {
            int count = RawEvents.Count;
            int last_hold = ActionIndex == 0 ? 0 : (int)RawEvents[ActionIndex - 1].X;
            for (int i = ActionIndex; i < count; i++)
            {
                int x = (int)RawEvents[i].X;
                long t = RawEvents[i].TimeStamp;

                for (int j = 0; j < Beatmap.Key; j++)
                {
                    bool is_holding = (x & 1) != 0;
                    bool last_is_holding = (last_hold & 1) != 0;

                    if (is_holding)
                    {
                        if (!last_is_holding)
                        {
                            // release -> hold: create new action
                            Action action = new Action
                            {
                                Column = j,
                                TimeStamp = t
                            };
                            bool is_ln = JudgeHold(action);
                            if (is_ln)
                            {
                                CurrentHolding[j] = action;
                                action.IsHolding = true;
                            }
                            Actions.Add(action);
                        }
                        else
                        {
                            // hold -> hold: do nothing
                            //if (CurrentHolding.ContainsKey(j))
                            //{
                            //    CurrentHolding[j].Duration = t - CurrentHolding[j].TimeStamp;
                            //}
                        }
                    }
                    else
                    {
                        if (last_is_holding)
                        {
                            // hold -> release: process only if it holds a LN
                            if (CurrentHolding.ContainsKey(j))
                            {
                                CurrentHolding[j].IsHolding = false;
                                CurrentHolding[j].Duration = t - CurrentHolding[j].TimeStamp;
                                JudgeRelease(CurrentHolding[j]);
                                CurrentHolding.Remove(j);
                            }
                        }
                    }

                    x /= 2;
                    last_hold /= 2;
                }

                last_hold = (int)RawEvents[i].X;
            }

            ActionIndex = count;

            int[] judgeCount = new int[6];
            if (Beatmap != null)
            {
                foreach (Note n in Beatmap.Notes)
                {
                    judgeCount[(int)n.Judgement] += 1;
                }
                Logger.E($"{judgeCount[0]} {judgeCount[1]} {judgeCount[2]} {judgeCount[3]} {judgeCount[4]} {judgeCount[5]} ");
            }
        }

    }
}
