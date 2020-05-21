using IpcLibrary;
using ManiaRTRender.Core;
using ManiaRTRender.Utils;
using OsuRTDataProvider.Listen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static ManiaRTRender.ManiaRTRenderPlugin;
using Action = ManiaRTRender.Core.Action;

namespace ManiaRTRender.Render
{
    // use ipc to send render commands to RenderClient
    public partial class RenderServer
    {
        private readonly Game _game;
        private readonly int _remoteId;
        private readonly RemoteRenderCommand _remoteRenderCommand;
        private readonly List<LineEvent> _lineEvents = new List<LineEvent>();
        private readonly List<RectEvent> _rectEvents = new List<RectEvent>();
        private string _playerName = "Unknown";

        private readonly LinkedList<Note> _notesToRender = new LinkedList<Note>();
        private readonly LinkedList<Action> _actionsToRender = new LinkedList<Action>();
        private long _preTime = long.MaxValue;
        private int _preActionsSize = 0;
        private bool _forceSync = false;

        private int _columnWidth = 0;
        private int _timeWindow;

        // Render parameters
        private static double COLUMN_PADDING_RATIO = 0.1;
        private static int TIME_INTERVAL = (int)Math.Round(1000.0 / 60);
        private static int HOLD_LOOSE = 500;
        private static readonly double HOLD_LOOSE_ALPHA = Math.Pow(1 / 255.0, 1.0 / HOLD_LOOSE);

        public RenderServer(Game game, int id)
        {
            this._game = game;

            // register IPC
            _remoteId = SerializeUtils.InitShareMemory(IpcConstants.GetRenderObjName(id), IpcConstants.SIZE_RENDER);
            _remoteRenderCommand = new RemoteRenderCommand();
            SendCommand();

            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
            path = Path.Combine(path, "RenderClient.exe");
            var process = new Process();
            var startInfo = new ProcessStartInfo(path, $"{id} {Process.GetCurrentProcess().Id}");
            process.StartInfo = startInfo;
            process.StartInfo.UseShellExecute = false;
            process.Start();

            Task.Run(() =>
            {
                try
                {
                    while (true)
                    {
                        FetchCommand();
                        while (!_remoteRenderCommand.RequestUpdate)
                        {
                            FetchCommand();
                        }
                        RenderGame();

                        _remoteRenderCommand.PlayerName = _playerName;
                        _remoteRenderCommand.RectEvents = _rectEvents;
                        _remoteRenderCommand.LineEvents = _lineEvents;
                        _remoteRenderCommand.RequestUpdate = false;
                        SendCommand();

                        Thread.Sleep(8);
                    }
                } catch (Exception e)
                {
                    Logger.E(e.StackTrace);
                }
                
            });
        }

        byte[] _buff = new byte[65536];
        private void SendCommand()
        {
            int length = _remoteRenderCommand.Write(ref _buff, 0);
            SerializeUtils.Save(_remoteId, ref _buff, length);
        }

        private void FetchCommand()
        {
            SerializeUtils.Fetch(_remoteId, ref _buff);
            _remoteRenderCommand.Read(ref _buff, 0);
        }

        public void OnPlayerChange(string player)
        {
            _playerName = player;
        }

        private void RenderGame()
        {
            _lineEvents.Clear();
            _rectEvents.Clear();
            if (_game.Status == GameStatus.Stop || _game.Beatmap == null)
            {
                _remoteRenderCommand.DrawBackground = true;
                return;
            }
            _remoteRenderCommand.DrawBackground = false;

            var key = _game.Beatmap.Key;
            _columnWidth = (int)((double)IpcConstants.GAME_WIDTH / key);
            _timeWindow = (int)((double)IpcConstants.GAME_HEIGHT / Setting.Speed * TIME_INTERVAL * _game.SpeedRatio);

            var time = _game.GetPlayingTime();
            if (time <= -10000 || time >= 1000000000) return;
            lock (_game.Actions)
            {
                if (time < _preTime || _forceSync)
                {
                    _notesToRender.CopyFrom(_game.Beatmap.Notes);
                    _actionsToRender.CopyFrom(_game.Actions);
                    _preActionsSize = _actionsToRender.Count;
                    _forceSync = false;
                }
                _preTime = time;
                if (_preActionsSize < _game.Actions.Count)
                {
                    _actionsToRender.AddSome(_game.Actions, _preActionsSize);
                }
                _preActionsSize = _game.Actions.Count;
            }

            var rawEvents = _game.RawEvents;

            // 0. draw judgement line
            _lineEvents.Add(new LineEvent
            {
                X1 = 0,
                Y1 = Setting.NoteHeight + 2,
                X2 = IpcConstants.GAME_WIDTH,
                Y2 = Setting.NoteHeight + 2,
                Width = 2,
                Color = Color.Red,
                Stipple = false
            });

            // 1. draw notes
            var hasHitNotes = false;
            FindRenderingNotes(_notesToRender, time, (note) =>
            {
                var dt = time - note.TimeStamp;
                var y = TimeToHeight(dt);
                Color color;
                if (dt <= _game.Beatmap.JudgementWindow[(int)Judgement.Miss] - Setting.ORTDPListenInterval 
                    && note.Judgement == Judgement.Miss)
                {
                    color = OsuUtils.COLOR_LIGHT;
                }
                else
                {
                    color = OsuUtils.JUDGEMENT_COLORS[(int)note.Judgement];
                    hasHitNotes |= note.Judgement != Judgement.Miss;
                }
                DrawNote(note.Column, y, (int)note.Duration, color, false, false);
            });

            // 2. draw action
            var hasActions = false;
            FindRenderingNotes(_actionsToRender, time, (action) =>
            {
                var dt = time - action.TimeStamp;
                var y = TimeToHeight(dt);
                DrawNote(action.Column, y, 0, OsuUtils.JUDGEMENT_COLORS[(int)action.JudgementStart], true, true);

                if (action.Duration != 0 || action.IsHolding)
                {
                    // LN
                    if (!action.IsHolding)
                    {
                        y = TimeToHeight(time - action.EndTime);
                        DrawNote(action.Column, y, 0, OsuUtils.JUDGEMENT_COLORS[(int)action.JudgementEnd], true, true);
                    }
                    DrawActionLN(action, time, OsuUtils.COLOR_LIGHT);
                }

                hasActions = true;
            });

            // 3. draw hold highlight
            try
            {
                var index = rawEvents.BinarySearch(time, (hitEvent) => hitEvent.TimeStamp);
                for (var i = 0; i < key; i++)
                {
                    for (var j = index; j >= 0 && time - rawEvents[j].TimeStamp <= HOLD_LOOSE; j--)
                    {
                        var hold = (int)rawEvents[j].X;

                        if ((hold & (1 << i)) == 0) continue;
                        var color = (int)(255.0 * Math.Pow(HOLD_LOOSE_ALPHA, time - rawEvents[j].TimeStamp));
                        if (j == index) color = 255;
                        color = Math.Min(Math.Max(0, color), 255);
                        DrawNote(i, Setting.NoteHeight, 0, Color.FromArgb(color, color, color), false, true);
                        break;
                    }
                }
            } catch (Exception ex)
            {
                Logger.E(ex.StackTrace);
            }

            _forceSync = hasHitNotes && !hasActions;
        }

        private void FindRenderingNotes<T>(LinkedList<T> notes, long time, OnFind<T> onFind) where T: BaseNote
        {
            var node = notes.First;
            var newNode = node.Next;
            lock (_game.Actions) {
                var extraTimeCount = 0;
                while (node != null)
                {
                    T t = node.Value;
                    var dt = time - t.TimeStamp;
                    if (dt < 0)
                    {
                        extraTimeCount += 1;
                        if (extraTimeCount >= 32)
                        {
                            break;
                        }
                    }
                    else
                    {
                        extraTimeCount = 0;
                    }

                    if (t.EndTime <= time - _timeWindow)
                    {
                        notes.Remove(node);
                    } 
                    else if (dt >= 0)
                    {
                        onFind(t);
                    }
                    node = newNode;
                }
            }
        }

        private int TimeToHeight(long t)
        {
            return (int)(((double)(t)) / _timeWindow * IpcConstants.GAME_HEIGHT);
        }

        private void DrawNote(int index, int y, int duration, Color color, bool isAction, bool shouldFill)
        {
            var x = (int)(index * _columnWidth);
            var h = Math.Max(Setting.NoteHeight, TimeToHeight(duration));
            var width = _columnWidth;
            if (isAction)
            {
                h = Setting.HitHeight;
                x += width / 5;
                width -= 2 * width / 5;
            }
            var yStart = y - h;
            if (y >= IpcConstants.GAME_HEIGHT) y = IpcConstants.GAME_HEIGHT;
            if (yStart <= 0) yStart = 0;
            var padding = (int)(_columnWidth * COLUMN_PADDING_RATIO);
            x += padding;
            width -= 2 * padding;

            _rectEvents.Add(new RectEvent
            {
                X1 = x,
                Y1 = yStart,
                X2 = x + width,
                Y2 = y,
                Color = color,
                ShouldFilled = shouldFill
            });
        }

        private void DrawActionLN(Action action, long currentTime, Color color)
        {
            var width = Setting.HitHeight;
            var x = (int)(action.Column * _columnWidth) + _columnWidth / 2;
            var y = TimeToHeight(currentTime - action.TimeStamp);
            var h = TimeToHeight(action.Duration);
            if (y < h || action.IsHolding) h = y;

            _lineEvents.Add(new LineEvent
            {
                X1 = x,
                Y1 = y - width,
                X2 = x,
                Y2 = y - h,
                Width = width,
                Color = color,
                Stipple = true
            });
        }

        private delegate void OnFind<T>(T obj);
    }
}