using IpcLibrary;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
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

        private long renderCount;
        private Stopwatch FpsStopwatch;
        private RemoteRenderCommand renderCommand = new RemoteRenderCommand();
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
            // In the same thread, don't worry for data hazard.
            FetchCommand();
            RequestUpdate();
            if (renderCommand.DrawBackground && hideInIdle)
            {
                Console.WriteLine("hide");

                container.Hide();
            }
            else
            {
                Console.WriteLine("show");
                container.Show();
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

        private long last = 0;
        void AppIdle(object sender, EventArgs e)
        {
            while (glControl.IsIdle)
            {

                while (!Setting.IsVSync && FpsStopwatch.ElapsedMilliseconds * Setting.FPS < 1000) ; // spin lock
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
        private void Render()
        {
            last = FpsStopwatch.ElapsedMilliseconds;
            if (Setting.SyncIfNeed())
            {
                glControl.VSync = Setting.IsVSync;
            }

            renderCount += 1;

            FetchCommand();

            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            GL.Clear(ClearBufferMask.ColorBufferBit);

            // notify server to render from now on

            if (renderCommand.RequestUpdate)
            {
                Console.WriteLine("Loss a frame!!!");
            }
            else
            {
                RequestUpdate();

                PlayerName = renderCommand.PlayerName;
                //Console.WriteLine($"{PlayerName}");

                RenderFrame();
            }

            Console.WriteLine($"{FpsStopwatch.ElapsedMilliseconds - last}");

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
            int length = dummyCommand.Write(ref buff, 0);
            SerializeUtils.Save(RemoteID, ref buff, length);
        }

        private void RenderFrame()
        {

            if (renderCommand.DrawBackground)
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

            var lineEvents = renderCommand.LineEvents;
            var rectEvents = renderCommand.RectEvents;

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