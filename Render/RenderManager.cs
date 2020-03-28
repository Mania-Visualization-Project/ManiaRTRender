using ManiaRTRender.Core;
using ManiaRTRender.Utils;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OsuRTDataProvider.Listen;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using static ManiaRTRender.ManiaRTRenderPlugin;
using Action = ManiaRTRender.Core.Action;

namespace ManiaRTRender.Render
{
    public partial class RenderManager
    {
        private GLUtils.ImageContext imageContext = new GLUtils.ImageContext();
        private GLControl glControl;
        private Game game;
        private long renderCount;
        private Stopwatch FpsStopwatch;

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

        public RenderManager(GLControl glControl, Game game)
        {
            this.glControl = glControl;
            this.game = game;
            this.FpsStopwatch = new Stopwatch();
            renderCount = 0;
        }

        public long GetRenderCountAndClear()
        {
            long count = renderCount;
            renderCount = 0;
            return count;
        }

        public void Load(EventArgs e)
        {
            glControl.Resize += new EventHandler(GLResize);
            glControl.Paint += new PaintEventHandler(GLPaint);

            GL.ClearColor(Color.Black);

            Application.Idle += AppIdle;
            GLResize(glControl, EventArgs.Empty);

            if (!Setting.IsVSync) FpsStopwatch.Start();
        }

        public void Close(CancelEventArgs e)
        {
            Application.Idle -= AppIdle;
        }

        void AppIdle(object sender, EventArgs e)
        {
            while (glControl.IsIdle)
            {
                while (!Setting.IsVSync && FpsStopwatch.ElapsedMilliseconds * Setting.FPS < 1000); // spin lock

                glControl.Invalidate();
                if (!Setting.IsVSync) FpsStopwatch.Restart();
            }
        }

        void GLResize(object sender, EventArgs e)
        {
            GLControl c = sender as GLControl;
            GLUtils.Resize(c);
        }

        void GLPaint(object sender, PaintEventArgs e)
        {
            Render();
        }

        private void Render()
        {

            renderCount += 1;

            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            GL.Clear(ClearBufferMask.ColorBufferBit);

            RenderGame();

            GL.Flush();
            glControl.SwapBuffers();

        }

        private void RenderGame()
        {
            if (game.Status == GameStatus.Stop || game.Beatmap == null)
            {
                GLUtils.DrawImage(Setting.BackgroundPicture, imageContext);
                return;
            }
            GLUtils.DisableImage(imageContext);

            int key = game.Beatmap.Key;
            columnWidth = (int)((double)GLUtils.GAME_WIDTH / key);
            timeWindow = (int)((double)GLUtils.GAME_HEIGHT / Setting.Speed * TIME_INTERVAL * game.SpeedRatio);

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
            GLUtils.DrawLine(0, Setting.NoteHeight + 2, GLUtils.GAME_WIDTH, Setting.NoteHeight + 2, 2, Color.Red, false);

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
            return (int)(((double)(t)) / timeWindow * GLUtils.GAME_HEIGHT);
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
            if (y >= GLUtils.GAME_HEIGHT) y = GLUtils.GAME_HEIGHT;
            if (yStart <= 0) yStart = 0;
            int padding = (int)(columnWidth * COLUMN_PADDING_RATIO);
            x += padding;
            width -= 2 * padding;

            GLUtils.DrawRect(x, yStart, x + width, y, color, shouldFill);
        }

        private void DrawActionLN(Action action, long currentTime, Color color)
        {
            int width = Setting.HitHeight;
            int x = (int)(action.Column * columnWidth) + columnWidth / 2;
            int y = TimeToHeight(currentTime - action.TimeStamp);
            int h = TimeToHeight(action.Duration);
            if (y < h || action.IsHolding) h = y;

            GLUtils.DrawLine(x, y - width, x, y - h, width, color, true);
        }

        private delegate void OnFind<T>(T obj);
    }
}