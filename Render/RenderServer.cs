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
        private Game _game;
        private int _remoteId = -1;
        private RemoteRenderCommand _remoteRenderCommand;
        private List<LineEvent> _lineEvents = new List<LineEvent>();
        private List<RectEvent> _rectEvents = new List<RectEvent>();
        private string _playerName = "Unknown";

        private LinkedList<Note> _notesToRender = new LinkedList<Note>();
        private LinkedList<Action> _actionsToRender = new LinkedList<Action>();
        private long _preTime = long.MaxValue;
        private int _preActionsSize = 0;
        private bool _forceSync = false;
        private FpsController _fpsController;

        private int _columnWidth = 0;
        private int _timeWindow;

        // Render parameters
        private static double COLUMN_PADDING_RATIO = 0.1;
        private static int TIME_INTERVAL = (int)Math.Round(1000.0 / 60);
        private static int HOLD_LOOSE = 500;
        private static double HOLD_LOOSE_ALPHA = Math.Pow(1 / 255.0, 1.0 / HOLD_LOOSE);

        public RenderServer(Game game, int id)
        {
            this._game = game;

            // register IPC
            _remoteId = SerializeUtils.InitShareMemory(IpcConstants.GetRenderObjName(id), IpcConstants.SIZE_RENDER);
            _remoteRenderCommand = new RemoteRenderCommand();
            SendCommand();

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
            path = Path.Combine(path, "RenderClient.exe");
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo(path, $"{id} {Process.GetCurrentProcess().Id}");
            process.StartInfo = startInfo;
            process.StartInfo.UseShellExecute = false;
            process.Start();
            int serverSleepPerCycle = Setting.ServerSleepPerCycle;
            _fpsController = new FpsController(120);

            Task.Run(() =>
            {
                try
                {
                    while (true)
                    {
                        FetchCommand();

                        if (_remoteRenderCommand.RequestUpdate)
                        {
                            long playingTime = RenderGame();

                            _remoteRenderCommand.PlayerName = _playerName;
                            _remoteRenderCommand.RectEvents = _rectEvents;
                            _remoteRenderCommand.LineEvents = _lineEvents;
                            _remoteRenderCommand.FallingSpeed = TimeToHeight(1);
                            _remoteRenderCommand.PlayingTime = (int)playingTime;
                            _remoteRenderCommand.RequestUpdate = false;
                            SendCommand();
                        }
                        
                        _fpsController.SpinFrame();
                    }
                } 
                catch (Exception e)
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

        private long RenderGame()
        {
            _lineEvents.Clear();
            _rectEvents.Clear();
            if (_game.Status == GameStatus.Stop || _game.Beatmap == null)
            {
                _remoteRenderCommand.DrawBackground = true;
                return -1;
            }
            _remoteRenderCommand.DrawBackground = false;

            int key = _game.Beatmap.Key;
            _columnWidth = (int)((double)IpcConstants.GAME_WIDTH / key);
            _timeWindow = (int)((double)IpcConstants.GAME_HEIGHT / Setting.Speed * TIME_INTERVAL * _game.SpeedRatio);

            long time = _game.GetPlayingTime();
            if (time <= -10000 || time >= 1000000000) return -1;
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

            List<HitEvent> rawEvents = _game.RawEvents;

            // 0. draw judgement line
            _lineEvents.Add(new LineEvent
            {
                X1 = 0,
                Y1 = Setting.NoteHeight + 2,
                X2 = IpcConstants.GAME_WIDTH,
                Y2 = Setting.NoteHeight + 2,
                Width = 2,
                Color = Color.Red,
                Stipple = false,
                Fallable = false
            });

            // 1. draw notes
            bool hasHitNotes = false;
            FindRenderingNotes(_notesToRender, time, (note) =>
            {
                long dt = time - note.TimeStamp;
                int y = TimeToHeight(dt);
                Color color;
                if (dt <= _game.Beatmap.JudgementWindow[(int)Judgement.MISS] - Setting.ORTDPListenInterval 
                    && note.Judgement == Judgement.MISS)
                {
                    color = OsuUtils.COLOR_LIGHT;
                }
                else
                {
                    color = OsuUtils.JUDGEMENT_COLORS[(int)note.Judgement];
                    hasHitNotes |= note.Judgement != Judgement.MISS;
                }
                DrawNote(note.Column, y, (int)note.Duration, color, false, false, true);
            });

            // 2. draw action
            bool hasActions = false;
            FindRenderingNotes(_actionsToRender, time, (action) =>
            {
                long dt = time - action.TimeStamp;
                int y = TimeToHeight(dt);
                DrawNote(action.Column, y, 0, OsuUtils.JUDGEMENT_COLORS[(int)action.JudgementStart], true, true, true);

                if (action.Duration != 0 || action.IsHolding)
                {
                    // LN
                    if (!action.IsHolding)
                    {
                        y = TimeToHeight(time - action.EndTime);
                        DrawNote(action.Column, y, 0, OsuUtils.JUDGEMENT_COLORS[(int)action.JudgementEnd], true, true, true);
                    }
                    DrawActionLN(action, time, OsuUtils.COLOR_LIGHT);
                }

                hasActions = true;
            });

            // 3. draw hold highlight
            try
            {
                int index = rawEvents.BinarySearch(time, (hitEvent) => hitEvent.TimeStamp);
                for (int i = 0; i < key; i++)
                {
                    for (int j = index; j >= 0 && time - rawEvents[j].TimeStamp <= HOLD_LOOSE; j--)
                    {
                        int hold = (int)rawEvents[j].X;
                        if ((hold & (1 << i)) == 0) continue;
                        int color = (int)(255.0 * Math.Pow(HOLD_LOOSE_ALPHA, time - rawEvents[j].TimeStamp));
                        if (j == index) color = 255;
                        color = Math.Min(Math.Max(0, color), 255);
                        DrawNote(i, Setting.NoteHeight, 0, Color.FromArgb(color, color, color), false, true, false);
                        break;
                    }
                }
            } 
            catch (Exception ex)
            {
                Logger.E(ex.StackTrace);
            }

            _forceSync = hasHitNotes && !hasActions;
            return time;
        }

        private void FindRenderingNotes<T>(LinkedList<T> notes, long time, OnFind<T> onFind) where T: BaseNote
        {
            lock (_game.Actions) {
                int extraTimeCount = 0;
                LinkedListNode<T> node = notes.First;
                while (node != null)
                {
                    T t = node.Value;
                    long dt = time - t.TimeStamp;
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

                    LinkedListNode<T> newNode = node.Next;
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
            return (int)((double)t / _timeWindow * IpcConstants.GAME_HEIGHT);
        }

        private void DrawNote(int index, int y, int duration, Color color, bool isAction, bool shouldFill, bool fallable = false)
        {
            int x = (int)(index * _columnWidth);
            int h = Math.Max(Setting.NoteHeight, TimeToHeight(duration));
            int width = _columnWidth;
            if (isAction)
            {
                h = Setting.HitHeight;
                x += width / 5;
                width -= 2 * width / 5;
            }
            int yStart = y - h;
            if (y >= IpcConstants.GAME_HEIGHT) y = IpcConstants.GAME_HEIGHT;
            if (yStart <= 0) yStart = 0;
            int padding = (int)(_columnWidth * COLUMN_PADDING_RATIO);
            x += padding;
            width -= 2 * padding;

            _rectEvents.Add(new RectEvent
            {
                X1 = x,
                Y1 = yStart,
                X2 = x + width,
                Y2 = y,
                Color = color,
                ShouldFilled = shouldFill,
                Fallable = fallable
            });
        }

        private void DrawActionLN(Action action, long currentTime, Color color)
        {
            int width = Setting.HitHeight;
            int x = (int)(action.Column * _columnWidth) + _columnWidth / 2;
            int y = TimeToHeight(currentTime - action.TimeStamp);
            int h = TimeToHeight(action.Duration);
            if (y < h || action.IsHolding) h = y;

            _lineEvents.Add(new LineEvent
            {
                X1 = x,
                Y1 = y - width,
                X2 = x,
                Y2 = y - h,
                Width = width,
                Color = color,
                Stipple = true,
                Fallable = true
            });
        }

        private delegate void OnFind<T>(T obj);
    }
}