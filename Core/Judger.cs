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
        private ManiaBeatmap _beatmap;

        public void Init(ManiaBeatmap beatmap)
        {
            _beatmap = beatmap;
            _notesToJudge.CopyFrom(beatmap.Notes);
            foreach (var note in _notesToJudge)
            {
                note.Judgable = true;
                note.Judged = false;
            }
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
            while (listNode != null)
            {
                Note note = listNode.Value;
                bool should_delete = false;
                if (note.Column == action.Column)
                {
                    long diff = action.TimeStamp - note.TimeStamp;
                    bool tooEarly = -diff > _judgementWindow[(int)Judgement.MISS]; // exclude early misses
                    bool tooLate = diff >= _judgementWindow[(int)Judgement.J_100];
                    if (note.Duration != 0)
                    {
                        diff = action.TimeStamp - note.EndTime;
                        tooLate = note.Judged
                            ? diff > -_judgementWindow[(int)Judgement.J_50]   // note was broken before
                            : diff >= _judgementWindow[(int)Judgement.J_100];
                    }
                    if (tooEarly)
                    {
                        break;
                    }
                    if (tooLate || !note.Judgable)
                    {
                        should_delete = true;
                    }
                    else // Hit
                    {
                        if (candidateNode == null)
                        {
                            candidateNode = listNode;
                            if (note.Duration == 0)
                            {
                                note.Judgable = false;
                                note.Judged = true;
                            }
                        }
                    }
                }
                var lastNode = listNode;
                listNode = listNode.Next;
                if (should_delete)
                {
                    _notesToJudge.Remove(lastNode);
                }
            }

            return candidateNode?.Value;
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

        private bool IsLNJudgedWith(long startDiff, long totalDiff, Judgement judgement, double rate)
        {
            var judgementTime = _judgementWindow[(int)judgement] * rate;
            return startDiff <= judgementTime && totalDiff <= judgementTime * 2;
        }

        private void JudgeRelease(Action action)
        {
            if (action.Target == null)
            {
                Logger.E("Action.Target == null!!!");
                return;
            }
            long endDiff = Math.Abs(action.EndTime - action.Target.EndTime);
            action.JudgementEnd = GetJudgement(endDiff / 1.5);

            // adjust target's judgement
            long startDiff = Math.Abs(action.TimeStamp - action.Target.TimeStamp);
            if (action.Target.TimeStamp - _judgementWindow[(int)Judgement.J_50] > action.TimeStamp)
            {
                // fxxking ppy
                long start = action.Target.EndTime - 1;
                startDiff = Math.Abs(action.Target.TimeStamp - start);
            }
            long totalDiff = startDiff + endDiff;

            Judgement totalJudgement;
            if (action.Target.EndTime - action.EndTime > _judgementWindow[(int)Judgement.J_50])
            {
                totalJudgement = Judgement.MISS;
            }
            else
            {
                action.Target.Judgable = false;
                if (IsLNJudgedWith(startDiff, totalDiff, Judgement.MAX, 1.2))
                {
                    totalJudgement = Judgement.MAX;
                }
                else if (IsLNJudgedWith(startDiff, totalDiff, Judgement.J_300, 1.1))
                {
                    totalJudgement = Judgement.J_300;
                }
                else if (IsLNJudgedWith(startDiff, totalDiff, Judgement.J_200, 1.0))
                {
                    totalJudgement = Judgement.J_200;
                }
                else if (IsLNJudgedWith(startDiff, totalDiff, Judgement.J_100, 1.0))
                {
                    totalJudgement = Judgement.J_100;
                }
                else
                {
                    totalJudgement = Judgement.J_50;
                }
            }
            if (action.Target.Judged && (totalJudgement == Judgement.MAX || totalJudgement == Judgement.J_300))
            {
                totalJudgement = Judgement.J_200;
            }
            action.Target.Judged = true;
            action.Target.Judgement = totalJudgement;
            //action.Target.HasEverReleased = true;
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
                                // hold -> hold: check if LN holding for too long
                                if (_currentHolding.ContainsKey(j) && _currentHolding[j].Target != null)
                                {
                                    var maxEndTimeEnable = _currentHolding[j].Target.EndTime + (long)_judgementWindow[(int)Judgement.J_50];
                                    if (t > maxEndTimeEnable)
                                    {
                                        ProcessLNRelease(j, t);
                                    }
                                }
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
            //if (_beatmap != null)
            //{
            //    foreach (Note n in _beatmap.Notes)
            //    {
            //        judgeCount[(int)n.Judgement] += 1;
            //    }
            //    Logger.E($"Judgement: {judgeCount[0]} {judgeCount[1]} {judgeCount[2]} {judgeCount[3]} {judgeCount[4]} {judgeCount[5]} ");
            //}
        }
    }
}
