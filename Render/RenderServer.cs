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
        private Game game;
        private int remoteId = -1;
        private RemoteRenderCommand remoteRenderCommand;
        private List<LineEvent> LineEvents = new List<LineEvent>();
        private List<RectEvent> RectEvents = new List<RectEvent>();
        private string PlayerName = "Unknown";

        private LinkedList<Note> notesToRender = new LinkedList<Note>();
        private LinkedList<Action> actionsToRender = new LinkedList<Action>();
        private long preTime = long.MaxValue;
        private int preActionsSize = 0;

        private int columnWidth = 0;
        private int timeWindow;

        // Render parameters
        private static double COLUMN_PADDING_RATIO = 0.1;
        private static int TIME_INTERVAL = (int)Math.Round(1000.0 / 60);
        private static int HOLD_LOOSE = 500;
        private static double HOLD_LOOSE_ALPHA = Math.Pow(1 / 255.0, 1.0 / HOLD_LOOSE);

        public RenderServer(Game game, int id)
        {
            this.game = game;

            // register IPC
            remoteId = SerializeUtils.InitShareMemory(IpcConstants.GetRenderObjName(id), IpcConstants.SIZE_RENDER);
            remoteRenderCommand = new RemoteRenderCommand();
            sendCommand();

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
            path = Path.Combine(path, "RenderClient.exe");
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo(path, $"{id} {Process.GetCurrentProcess().Id}");
            process.StartInfo = startInfo;
            process.StartInfo.UseShellExecute = false;
            process.Start();

            Task.Run(() =>
            {
                try
                {
                    while (true)
                    {
                        fetchCommand();
                        while (!remoteRenderCommand.RequestUpdate)
                        {
                            fetchCommand();
                        }
                        RenderGame();

                        remoteRenderCommand.PlayerName = PlayerName;
                        remoteRenderCommand.RectEvents = RectEvents;
                        remoteRenderCommand.LineEvents = LineEvents;
                        remoteRenderCommand.RequestUpdate = false;
                        sendCommand();

                        Thread.Sleep(8);
                    }
                } catch (Exception e)
                {
                    Logger.E(e.StackTrace);
                }
                
            });
        }

        byte[] buff = new byte[65536];
        private void sendCommand()
        {
            int length = remoteRenderCommand.Write(ref buff, 0);
            SerializeUtils.Save(remoteId, ref buff, length);
        }

        private void fetchCommand()
        {
            SerializeUtils.Fetch(remoteId, ref buff);
            remoteRenderCommand.Read(ref buff, 0);
        }

        public void OnPlayerChange(string player)
        {
            PlayerName = player;
        }

        private void RenderGame()
        {
            LineEvents.Clear();
            RectEvents.Clear();
            if (game.Status == GameStatus.Stop || game.Beatmap == null)
            {
                remoteRenderCommand.DrawBackground = true;
                return;
            }
            remoteRenderCommand.DrawBackground = false;

            int key = game.Beatmap.Key;
            columnWidth = (int)((double)IpcConstants.GAME_WIDTH / key);
            timeWindow = (int)((double)IpcConstants.GAME_HEIGHT / Setting.Speed * TIME_INTERVAL * game.SpeedRatio);

            long time = game.GetPlayingTime();
            if (time <= -10000 || time >= 1000000000) return;
            if (time < preTime)
            {
                notesToRender.CopyFrom(game.Beatmap.Notes);
                actionsToRender.CopyFrom(game.Actions);
                preActionsSize = actionsToRender.Count;
            }
            preTime = time;
            if (preActionsSize < game.Actions.Count)
            {
                actionsToRender.AddSome(game.Actions, preActionsSize);
            }
            preActionsSize = game.Actions.Count;

            List<HitEvent> rawEvents = game.RawEvents;

            // 0. draw judgement line
            LineEvents.Add(new LineEvent
            {
                x1 = 0,
                y1 = Setting.NoteHeight + 2,
                x2 = IpcConstants.GAME_WIDTH,
                y2 = Setting.NoteHeight + 2,
                width = 2,
                color = Color.Red,
                stipple = false
            });

            // 1. draw notes
            FindRenderingNotes(notesToRender, time, (note) =>
            {
                long dt = time - note.TimeStamp;
                int y = TimeToHeight(dt);
                Color color;
                if (dt <= game.Beatmap.JudgementWindow[(int)Judgement.MISS] - Setting.ORTDPListenInterval 
                    && note.Judgement == Judgement.MISS)
                {
                    color = OsuUtils.COLOR_LIGHT;
                }
                else
                {
                    color = OsuUtils.JUDGEMENT_COLORS[(int)note.Judgement];
                }
                DrawNote(note.Column, y, (int)note.Duration, color, false, false);
            });

            // 2. draw action
            FindRenderingNotes(actionsToRender, time, (action) =>
            {
                long dt = time - action.TimeStamp;
                int y = TimeToHeight(dt);
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

                        if ((hold & (1 << i)) != 0)
                        {
                            int color = (int)(255.0 * Math.Pow(HOLD_LOOSE_ALPHA, time - rawEvents[j].TimeStamp));
                            if (j == index) color = 255;
                            color = Math.Min(Math.Max(0, color), 255);
                            DrawNote(i, Setting.NoteHeight, 0, Color.FromArgb(color, color, color), false, true);
                            break;
                        }
                    }
                }
            } catch (Exception ex)
            {
                Logger.E(ex.StackTrace);
            }
        }

        private void FindRenderingNotes<T>(LinkedList<T> notes, long time, OnFind<T> onFind) where T: BaseNote
        {
            LinkedListNode<T> node = notes.First;
            while (node != null)
            {
                T t = node.Value;
                long dt = time - t.TimeStamp;
                if (dt < 0) break;
                onFind(t);
                
                LinkedListNode<T> newNode = node.Next;
                if (t.EndTime <= time - timeWindow)
                {
                    notes.Remove(node);
                }
                node = newNode;
            }
        }

        private int TimeToHeight(long t)
        {
            return (int)(((double)(t)) / timeWindow * IpcConstants.GAME_HEIGHT);
        }

        private void DrawNote(int index, int y, int duration, Color color, bool isAction, bool shouldFill)
        {
            int x = (int)(index * columnWidth);
            int h = Math.Max(Setting.NoteHeight, TimeToHeight(duration));
            int width = columnWidth;
            if (isAction)
            {
                h = Setting.HitHeight;
                x += width / 5;
                width -= 2 * width / 5;
            }
            int yStart = y - h;
            if (y >= IpcConstants.GAME_HEIGHT) y = IpcConstants.GAME_HEIGHT;
            if (yStart <= 0) yStart = 0;
            int padding = (int)(columnWidth * COLUMN_PADDING_RATIO);
            x += padding;
            width -= 2 * padding;

            RectEvents.Add(new RectEvent
            {
                x1 = x,
                y1 = yStart,
                x2 = x + width,
                y2 = y,
                color = color,
                shouldFilled = shouldFill
            });
        }

        private void DrawActionLN(Action action, long currentTime, Color color)
        {
            int width = Setting.HitHeight;
            int x = (int)(action.Column * columnWidth) + columnWidth / 2;
            int y = TimeToHeight(currentTime - action.TimeStamp);
            int h = TimeToHeight(action.Duration);
            if (y < h || action.IsHolding) h = y;

            LineEvents.Add(new LineEvent
            {
                x1 = x,
                y1 = y - width,
                x2 = x,
                y2 = y - h,
                width = width,
                color = color,
                stipple = true
            });
        }

        private delegate void OnFind<T>(T obj);
    }
}