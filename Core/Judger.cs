using ManiaRTRender.Utils;
using OsuRTDataProvider.Listen;
using System;
using System.Collections.Generic;
using static ManiaRTRender.ManiaRTRenderPlugin;

namespace ManiaRTRender.Core
{
    public class Judger
    {

        LinkedList<Note> NotesToJudge = new LinkedList<Note>();
        private int ActionIndex = 0;
        private int Key;
        double[] JudgementWindow;
        private Dictionary<int, Action> CurrentHolding = new Dictionary<int, Action>();


        public void init(ManiaBeatmap beatmap)
        {
            NotesToJudge.CopyFrom(beatmap.Notes);
            JudgementWindow = beatmap.JudgementWindow;
            Key = beatmap.Key;
            ActionIndex = 0;
            CurrentHolding.Clear();
        }

        private Judgement GetJudgement(double diff)
        {
            for (int i = 0; i < JudgementWindow.Length; i++)
            {
                if (diff <= JudgementWindow[i])
                {
                    return (Judgement)i;
                }
            }
            return Judgement.MISS;
        }

        private Note FindTarget(Action action)
        {
            LinkedListNode<Note> list_node = NotesToJudge.First;
            LinkedListNode<Note> candidate_node = null;
            long candidate_diff = 0;
            while (list_node != null)
            {
                Note note = list_node.Value;
                if (note.Column == action.Column)
                {
                    long diff = action.TimeStamp - note.TimeStamp;
                    bool tooEarly = -diff > JudgementWindow[(int)Judgement.MISS]; // exclude early misses
                    bool tooLate = diff > JudgementWindow[(int)Judgement.J_50];
                    if (tooEarly) break;
                    bool hit = !tooEarly && !tooLate;
                    if (hit)
                    {
                        if (candidate_node == null || (diff > 0 && candidate_diff > 0))
                        {
                            candidate_node = list_node;
                            candidate_diff = diff;
                        }
                    }
                }
                list_node = list_node.Next;
            }

            if (candidate_node != null)
            {
                Note note = candidate_node.Value;
                NotesToJudge.Remove(candidate_node);
                return note;
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
            long end_diff = Math.Abs(action.EndTime - action.Target.EndTime);
            end_diff = (long)(end_diff / 1.5); // LN lenience
            action.JudgementEnd = GetJudgement(end_diff);

            // adjust target's judgement
            long start_diff = Math.Abs(action.TimeStamp - action.Target.TimeStamp);
            long diff = (end_diff + start_diff) / 2;
            Judgement judgement = GetJudgement(diff);
            if (judgement == Judgement.MISS)
            {
                judgement = end_diff > 0 ? Judgement.J_100 : Judgement.J_50;
            }
            action.Target.Judgement = judgement;
        }

        public bool ShouldReset(List<HitEvent> rawEvents)
        {
            return ActionIndex > rawEvents.Count;
        }

        public void TryToJudge(List<HitEvent> rawEvents, List<Action> actions)
        {
            lock (this)
            {
                int count = rawEvents.Count;
                int last_hold = ActionIndex == 0 ? 0 : (int)rawEvents[ActionIndex - 1].X;
                for (int i = ActionIndex; i < count; i++)
                {
                    int x = (int)rawEvents[i].X;
                    long t = rawEvents[i].TimeStamp;

                    for (int j = 0; j < Key; j++)
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
                                    action.Duration = 30000000L; // mark a very long value
                                }
                                actions.Add(action);
                            }
                            else
                            {
                                // hold -> hold: do nothing
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

                    last_hold = (int)rawEvents[i].X;
                }

                ActionIndex = count;
            }
            

            // for debug
            //int[] judgeCount = new int[6];
            //if (Beatmap != null)
            //{
            //    foreach (Note n in Beatmap.Notes)
            //    {
            //        judgeCount[(int)n.Judgement] += 1;
            //    }
            //    Logger.E($"Judgement: {judgeCount[0]} {judgeCount[1]} {judgeCount[2]} {judgeCount[3]} {judgeCount[4]} {judgeCount[5]} ");
            //}
        }
    }
}
