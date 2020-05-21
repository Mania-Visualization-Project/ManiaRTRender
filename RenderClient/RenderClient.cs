﻿using IpcLibrary;
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
        private readonly GLUtils.ImageContext _imageContext = new GLUtils.ImageContext();
        private readonly GLUtils.ImageContext _imageForegroundContext = new GLUtils.ImageContext();
        private readonly GLControl _glControl;
        private readonly Form _container;

        private bool _hideInIdle;
        private bool _isHidden;

        private long _renderCount;
        private readonly Stopwatch _fpsStopwatch;
        private readonly RemoteRenderCommand _renderCommand = new RemoteRenderCommand();
        private readonly RemoteRenderCommand _preCommand = new RemoteRenderCommand();
        private RemoteRenderCommand _targetCommand;

        private readonly int _remoteID;
            
        public string PlayerName { get; private set; }

        public RenderClient(GLControl glControl, Form container, int id)
        {
            _glControl = glControl;
            _container = container;
            _fpsStopwatch = new Stopwatch();
            _renderCount = 0;

            _remoteID = SerializeUtils.InitShareMemory(IpcConstants.GetRenderObjName(id), IpcConstants.SIZE_RENDER);
            PlayerName = "Unknown";
        }

        public void SetHideInIdle(bool hideInIdle)
        {
            _hideInIdle = hideInIdle;
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

            var count = _renderCount;
            _renderCount = 0;
            return count;
        }

        public void Load(EventArgs e)
        {
            _glControl.Resize += GLResize;
            _glControl.Paint += GLPaint;

            GL.ClearColor(Color.Black);

            Application.Idle += AppIdle;
            GLResize(_glControl, EventArgs.Empty);

            if (!Setting.IsVSync) _fpsStopwatch.Start();
        }

        public void Close(CancelEventArgs e)
        {
            Application.Idle -= AppIdle;
        }

        private void AppIdle(object sender, EventArgs e)
        {
            while (_glControl.IsIdle)
            {
                if (!Setting.IsVSync) {
                    SpinWait.SpinUntil(() =>  _fpsStopwatch.ElapsedMilliseconds * Setting.FPS >= 1000);
                }
                _glControl.Invalidate();
                if (!Setting.IsVSync) _fpsStopwatch.Restart();
            }
        }

        private void GLResize(object sender, EventArgs e)
        {
            var c = sender as GLControl;
            GLUtils.Resize(c);
        }

        private void GLPaint(object sender, PaintEventArgs e)
        {
            Render();
        }

        private byte[] _buff = new byte[65536];
        private byte[] _responseBuff = new byte[4096];

        private void Render()
        {
            if (Setting.SyncIfNeed())
            {
                _glControl.VSync = Setting.IsVSync;
            }

            _renderCount += 1;

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
            var dummyCommand = new RemoteRenderCommand {RequestUpdate = true};
            var length = dummyCommand.Write(ref _responseBuff, 0);
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

            var lineEvents = _targetCommand.LineEvents;
            var rectEvents = _targetCommand.RectEvents;

            foreach (var lineEvent in lineEvents)
            {
                GLUtils.DrawLine(lineEvent.X1, lineEvent.Y1, lineEvent.X2, lineEvent.Y2,
                    lineEvent.Width, lineEvent.Color, lineEvent.Stipple);
            }
            foreach (var rectEvent in rectEvents)
            {
                GLUtils.DrawRect(rectEvent.X1, rectEvent.Y1, rectEvent.X2, rectEvent.Y2,
                    rectEvent.Color, rectEvent.ShouldFilled);
            }
        }
    }
}