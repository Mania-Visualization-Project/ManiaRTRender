using IpcLibrary;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace RenderClient
{
    public partial class RenderClient
    {
        private GLUtils.ImageContext imageContext = new GLUtils.ImageContext();
        private GLUtils.ImageContext imageForegroundContext = new GLUtils.ImageContext();
        private GLControl glControl;
        private Form container;

        private bool hideInIdle = false;
        private bool isHidden = false;

        private long renderCount;
        private Stopwatch FpsStopwatch;
        private RemoteRenderCommand renderCommand = new RemoteRenderCommand();
        private RemoteRenderCommand preCommand = new RemoteRenderCommand();
        private RemoteRenderCommand targetCommand = null;

        private int RemoteID = -1;
            
        public string PlayerName { get; private set; }

        public RenderClient(GLControl glControl, Form container, int id)
        {
            this.glControl = glControl;
            this.container = container;
            this.FpsStopwatch = new Stopwatch();
            renderCount = 0;

            RemoteID = SerializeUtils.InitShareMemory(IpcConstants.GetRenderObjName(id), IpcConstants.SIZE_RENDER);
            PlayerName = "Unknown";
        }

        public void SetHideInIdle(bool hideInIdle)
        {
            this.hideInIdle = hideInIdle;
        }

        public long GetRenderCountAndClear()
        {
            // hide form if need
            if (isHidden)
            {
                // In the same thread, don't worry for data hazard.
                FetchCommand();
                RequestUpdate();
            }
            if (renderCommand.DrawBackground && hideInIdle)
            {
                Console.WriteLine("hide");
                container.Hide();
                isHidden = true;
            }
            else
            {
                Console.WriteLine("show");
                container.Show();
                isHidden = false;
            }

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

                if (!Setting.IsVSync) {
                    SpinWait.SpinUntil(
                        () =>  FpsStopwatch.ElapsedMilliseconds * Setting.FPS >= 1000
                    );
                }
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

        private byte[] buff = new byte[65536];
        private byte[] responseBuff = new byte[4096];

        private void Render()
        {
            if (Setting.SyncIfNeed())
            {
                glControl.VSync = Setting.IsVSync;
            }

            renderCount += 1;

            FetchCommand();
            targetCommand = renderCommand;

            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            GL.Clear(ClearBufferMask.ColorBufferBit);

            // notify server to render from now on

            if (targetCommand.RequestUpdate)
            {
                Console.WriteLine("Loss a frame!!!");
                // render with precious command to avoid flash
                targetCommand = preCommand;
            }

            if (!targetCommand.RequestUpdate)
            {
                RequestUpdate();

                PlayerName = renderCommand.PlayerName;
                //Console.WriteLine($"{PlayerName}");

                RenderFrame();

                if (targetCommand == renderCommand)
                {
                    preCommand.Read(ref buff, 0);
                }
            }

            GL.Flush();
            glControl.SwapBuffers();
        }

        private void FetchCommand()
        {
            SerializeUtils.Fetch(RemoteID, ref buff);
            renderCommand.Read(ref buff, 0);
        }

        private void RequestUpdate()
        {
            RemoteRenderCommand dummyCommand = new RemoteRenderCommand();
            dummyCommand.RequestUpdate = true;
            int length = dummyCommand.Write(ref responseBuff, 0);
            SerializeUtils.Save(RemoteID, ref responseBuff, length);
        }

        private void RenderFrame()
        {
            if (targetCommand == null) return;

            if (targetCommand.DrawBackground)
            {
                GLUtils.DrawImage(Setting.BackgroundPicture, imageContext);
                return;
            }

            GLUtils.DisableImage(imageContext);

            if (Setting.BackgroundPictureInPlaying != "")
            {
                GLUtils.DrawImage(Setting.BackgroundPictureInPlaying, imageForegroundContext);
            }
            else
            {
                GLUtils.DisableImage(imageForegroundContext);
            }

            var lineEvents = targetCommand.LineEvents;
            var rectEvents = targetCommand.RectEvents;

            foreach (var lineEvent in lineEvents)
            {
                GLUtils.DrawLine(lineEvent.x1, lineEvent.y1, lineEvent.x2, lineEvent.y2,
                    lineEvent.width, lineEvent.color, lineEvent.stipple);
            }
            foreach (var rectEvent in rectEvents)
            {
                GLUtils.DrawRect(rectEvent.x1, rectEvent.y1, rectEvent.x2, rectEvent.y2,
                    rectEvent.color, rectEvent.shouldFilled);
            }
        }
    }
}