using System;
using System.Diagnostics;

namespace IpcLibrary
{
    public class FpsController
    {
        private double _frameCountTarget;
        private long _baseTime = 0;
        private long _fpsStartTime = -1;
        private long _fpsCount = 0;
        private Stopwatch _stopwatch = new Stopwatch();
        private readonly double BASE_TIME = 100.0;

        public FpsController(int fps)
        {
            _frameCountTarget = fps / (1000 / BASE_TIME);
            _stopwatch.Start();
        }

        public void Reset()
        {
            _baseTime = System.Diagnostics.Stopwatch.GetTimestamp();
        }

        public long SpinFrame()
        {
            long currentTime = _stopwatch.ElapsedMilliseconds;
            if (_fpsStartTime < 0 || _fpsCount >= _frameCountTarget)
            {
                _fpsCount = 0;
                _fpsStartTime = currentTime;
            }
            int sleepTime = (int)Math.Round((BASE_TIME - (currentTime - _fpsStartTime)) / (_frameCountTarget - _fpsCount));
            if (sleepTime >= 0)
                System.Threading.Thread.Sleep(sleepTime);
            _fpsCount++;
            return sleepTime;
        }
    }
}