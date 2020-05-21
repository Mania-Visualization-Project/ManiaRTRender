using ManiaRTRender.Utils;
using OsuRTDataProvider.Listen;
using System;
using System.Collections.Generic;
using static ManiaRTRender.ManiaRTRenderPlugin;

namespace ManiaRTRender.Core
{
    public class Judger
    {

        private LinkedList<Note> _notesToJudge = new LinkedList<Note>();
        private int _actionIndex = 0;
        private int _key;
        private double[] _judgementWindow;
        private Dictionary<int, Action> _currentHolding = new Dictionary<int, Action>();


        public void Init(ManiaBeatmap beatmap)
        {
            _notesToJudge.CopyFrom(beatmap.Notes);
            _judgementWindow = beatmap.JudgementWindow;
            _key = beatmap.Key;
            _actionIndex = 0;
            _currentHolding.Clear();
        }

        private Judgement GetJudgement(double diff)
        {
            for (var i = 0; i < _judgementWindow.Length; i++)
            {
                if (diff <= _judgementWindow[i])
                {
                    return (Judgement)i;
                }
            }
            return Judgement.MISS;
        }

        private Note FindTarget(Action action)
        {
            LinkedListNode<Note> listNode = _notesToJudge.First;
            LinkedListNode<Note> candidateNode = null;
            long candidateDiff = 0;
            while (listNode != null)
            {
                Note note = listNode.Value;
                if (note.Column == action.Column)
                {
                    long diff = action.TimeStamp - note.TimeStamp;
                    bool tooEarly = -diff > _judgementWindow[(int)Judgement.MISS]; // exclude early misses
                    bool tooLate = diff > _judgementWindow[(int)Judgement.J_50];
                    if (tooEarly) break;
                    bool hit = !tooLate;
                    if (hit)
                    {
                        if (candidateNode == null || (diff > 0 && candidateDiff > 0))
                        {
                            candidateNode = listNode;
                            candidateDiff = diff;
                        }
                    }
                }
                listNode = listNode.Next;
            }

            if (candidateNode != null)
            {
                Note note = candidateNode.Value;
                _notesToJudge.Remove(candidateNode);
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
            long endDiff = Math.Abs(action.EndTime - action.Target.EndTime);
            endDiff = (long)(endDiff / 1.5); // LN lenience
            action.JudgementEnd = GetJudgement(endDiff);

            // adjust target's judgement
            long startDiff = Math.Abs(action.TimeStamp - action.Target.TimeStamp);
            long diff = (endDiff + startDiff) / 2;
            Judgement judgement = GetJudgement(diff);
            if (judgement == Judgement.MISS)
            {
                judgement = endDiff > 0 ? Judgement.J_100 : Judgement.J_50;
            }
            action.Target.Judgement = judgement;
        }

        public bool ShouldReset(List<HitEvent> rawEvents)
        {
            return _actionIndex > rawEvents.Count;
        }

        private void ProcessLNRelease(int column, long t)
        {
            if (!_currentHolding.ContainsKey(column)) return;
            _currentHolding[column].IsHolding = false;
            long duration = t - _currentHolding[column].TimeStamp;
            long maxDuration = _currentHolding[column].Target.Duration + (long)_judgementWindow[(int)Judgement.MISS];
            if (duration >= maxDuration)
            {
                duration = maxDuration;
            }
            _currentHolding[column].Duration = duration;
            JudgeRelease(_currentHolding[column]);
            _currentHolding.Remove(column);
        }

        public void TryToJudge(List<HitEvent> rawEvents, List<Action> actions)
        {
            lock (actions)
            {
                int count = rawEvents.Count;
                int lastHold = _actionIndex == 0 ? 0 : (int)rawEvents[_actionIndex - 1].X;
                for (int i = _actionIndex; i < count; i++)
                {
                    int x = (int)rawEvents[i].X;
                    long t = rawEvents[i].TimeStamp;

                    for (int j = 0; j < _key; j++)
                    {
                        bool isHolding = (x & 1) != 0;
                        bool lastIsHolding = (lastHold & 1) != 0;

                        if (isHolding)
                        {
                            if (!lastIsHolding)
                            {
                                // release -> hold: create new action
                                Action action = new Action
                                {
                                    Column = j,
                                    TimeStamp = t
                                };
                                bool isLn = JudgeHold(action);
                                if (isLn)
                                {
                                    lock (_currentHolding)
                                    {
                                        ProcessLNRelease(j, t);
                                        _currentHolding[j] = action;
                                    }
                                    action.IsHolding = true;
                                    action.Duration = action.Target.Duration + (long)_judgementWindow[(int)Judgement.MISS];
                                }
                                lock (actions)
                                {
                                    actions.Add(action);
                                }
                            }
                            else
                            {
                                // hold -> hold: do nothing
                            }
                        }
                        else
                        {
                            if (lastIsHolding)
                            {
                                // hold -> release: process only if it holds a LN
                                lock (_currentHolding)
                                {
                                    ProcessLNRelease(j, t);
                                }
                            }
                        }

                        x /= 2;
                        lastHold /= 2;
                    }

                    lastHold = (int)rawEvents[i].X;
                }

                _actionIndex = count;
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
