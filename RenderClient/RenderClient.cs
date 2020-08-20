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
        private GLUtils.ImageContext _imageContext = new GLUtils.ImageContext();
        private GLUtils.ImageContext _imageForegroundContext = new GLUtils.ImageContext();
        private GLControl _glControl;
        private Form _container;

        private bool _hideInIdle = false;
        private bool _isHidden = false;

        private long _renderCount;
        private long _renderTotalCount;
        private Stopwatch _fpsStopwatch;
        private RemoteRenderCommand _renderCommand = new RemoteRenderCommand();
        private RemoteRenderCommand _preCommand = new RemoteRenderCommand();
        private RemoteRenderCommand _targetCommand = null;

        private FpsController _fpsController;
        private int _basePlayingTime = int.MaxValue;
        private long _baseSystemTime = long.MaxValue;

        private int _remoteID = -1;
            
        public string PlayerName { get; private set; }

        public RenderClient(GLControl glControl, Form container, int id)
        {
            _glControl = glControl;
            _container = container;

            if (Setting.SyncIfNeed())
            {
                _glControl.VSync = Setting.IsVSync;
                if (!Setting.IsVSync)
                {
                    _fpsController = new FpsController(Setting.FPS);
                }
            }
            _fpsStopwatch = new Stopwatch();
            _fpsStopwatch.Start();
            _renderCount = 0;
            _renderTotalCount = 0;

            _remoteID = SerializeUtils.InitShareMemory(IpcConstants.GetRenderObjName(id), IpcConstants.SIZE_RENDER);
            PlayerName = "Unknown";
        }

        public void SetHideInIdle(bool hideInIdle)
        {
            this._hideInIdle = hideInIdle;
        }

        public long GetRenderCountAndClear()
        {
            // hide form if need
            if (_isHidden)
            {
                // In the same thread, don't worry for data hazard.
                FetchCommand();
                RequestUpdate();
            }
            if (_renderCommand.DrawBackground && _hideInIdle)
            {
                Console.WriteLine("hide");
                _container.Hide();
                _isHidden = true;
            }
            else
            {
                Console.WriteLine("show");
                _container.Show();
                _isHidden = false;
            }

            long count = _renderCount;
            _renderCount = 0;
            return count;
        }

        public void Load(EventArgs e)
        {
            _glControl.Resize += new EventHandler(GLResize);
            _glControl.Paint += new PaintEventHandler(GLPaint);

            GL.ClearColor(Color.Black);

            Application.Idle += AppIdle;
            GLResize(_glControl, EventArgs.Empty);

            _fpsStopwatch.Start();
        }

        public void Close(CancelEventArgs e)
        {
            Application.Idle -= AppIdle;
        }

        void AppIdle(object sender, EventArgs e)
        {
            while (_glControl.IsIdle)
            {

                if (!Setting.IsVSync) {
                    _fpsController.SpinFrame();
                    //while (_fpsStopwatch.ElapsedMilliseconds * Setting.FPS <= 1000) ;
                    //SpinWait.SpinUntil(
                    //    () => _fpsStopwatch.ElapsedMilliseconds * Setting.FPS >= 1000
                    //);
                    //Thread.Sleep(1000 / Setting.FPS);
                }
                _glControl.Invalidate();
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

        private byte[] _buff = new byte[65536];
        private byte[] _responseBuff = new byte[4096];

        private void Render()
        {
            _renderCount += 1;
            _renderTotalCount += 1;

            FetchCommand();
            _targetCommand = _renderCommand;

            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            GL.Clear(ClearBufferMask.ColorBufferBit);

            // notify server to render from now on

            if (_targetCommand.RequestUpdate)
            {
                Console.WriteLine("Loss a frame!!!");
                // render with precious command to avoid flash
                _targetCommand = _preCommand;
            }

            if (!_targetCommand.RequestUpdate)
            {
                RequestUpdate();

                PlayerName = _renderCommand.PlayerName;
                //Console.WriteLine($"{PlayerName}");

                RenderFrame();

                if (_targetCommand == _renderCommand)
                {
                    _preCommand.Read(ref _buff, 0);
                }
            }

            GL.Flush();
            _glControl.SwapBuffers();
        }

        private void FetchCommand()
        {
            SerializeUtils.Fetch(_remoteID, ref _buff);
            _renderCommand.Read(ref _buff, 0);
        }

        private void RequestUpdate()
        {
            RemoteRenderCommand dummyCommand = new RemoteRenderCommand();
            dummyCommand.RequestUpdate = true;
            int length = dummyCommand.Write(ref _responseBuff, 0);
            SerializeUtils.Save(_remoteID, ref _responseBuff, length);
        }

        private void RenderFrame()
        {
            if (_targetCommand == null) return;

            if (_targetCommand.DrawBackground)
            {
                GLUtils.DrawImage(Setting.BackgroundPicture, _imageContext);
                return;
            }

            GLUtils.DisableImage(_imageContext);

            if (Setting.BackgroundPictureInPlaying != "")
            {
                GLUtils.DrawImage(Setting.BackgroundPictureInPlaying, _imageForegroundContext);
            }
            else
            {
                GLUtils.DisableImage(_imageForegroundContext);
            }

            var currentPlayingTime = _targetCommand.PlayingTime;
            var currentSystemTime = _fpsStopwatch.ElapsedMilliseconds;
            var expectedPlayingTime = _basePlayingTime + (currentSystemTime - _baseSystemTime);
            var offset = (int)((expectedPlayingTime - currentPlayingTime) * _targetCommand.FallingSpeed);
            if (currentPlayingTime != _basePlayingTime)
            {
                _basePlayingTime = currentPlayingTime;
                _baseSystemTime = currentSystemTime;
            }

            var lineEvents = _targetCommand.LineEvents;
            var rectEvents = _targetCommand.RectEvents;

            foreach (var lineEvent in lineEvents)
            {
                GLUtils.DrawLine(
                    lineEvent.X1, Fall(lineEvent.Y1, lineEvent.Fallable, offset),
                    lineEvent.X2, Fall(lineEvent.Y2, lineEvent.Fallable, offset),
                    lineEvent.Width, lineEvent.Color, lineEvent.Stipple);
            }
            foreach (var rectEvent in rectEvents)
            {
                GLUtils.DrawRect(
                    rectEvent.X1, Fall(rectEvent.Y1, rectEvent.Fallable, offset),
                    rectEvent.X2, Fall(rectEvent.Y2, rectEvent.Fallable, offset),
                    rectEvent.Color, rectEvent.ShouldFilled);
            }
        }

        private int Fall(int y, bool fallable, int offset)
        {
            if (!fallable || y == 0) return y;
            var y2 = y + offset;
            if (y2 >= IpcConstants.GAME_HEIGHT) return IpcConstants.GAME_HEIGHT;
            return y2;
        }
    }
}