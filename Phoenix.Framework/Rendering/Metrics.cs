using System.Data;

namespace Phoenix.Framework.Rendering
{
    public class Metrics
    {
        public double Time { get; private set; } = 0;
        public double FrameTime { get; private set; } = 0;
        public int FT_SAMPLE { get; private set; } = 0;
        public double FT_SAMPLE_RATE { get; set; } = 0.25;
        public double UPS { get; private set; } = 0;
        public double UPS_SAMPLE_RATE{ get; set; } = 0.25;
        public int UPS_SAMPLE { get; private set; } = 0;
        public double FPS { get; private set; } = 0;
        public int FPS_SAMPLE { get; private set; } = 0;
        public double FPS_SAMPLE_RATE { get; set; } = 0.25;

        double _timerSamplerFPS = 0;
        double _timerSamplerFT = 0;
        double _timerSamplerUPS = 0;
        internal void ProcessUpdate(double dt)
        {
            UPS = dt;
            Time += dt;
            _timerSamplerUPS += dt;
            if(_timerSamplerUPS >= UPS_SAMPLE_RATE)
            {
                UPS_SAMPLE = (int)(1.0 / UPS);
                _timerSamplerUPS = 0;
            }
        }
        internal void ProcessRender(double dt)
        {
            FrameTime = dt;

            FPS = 1.0 / dt;
            _timerSamplerFPS += dt;
            _timerSamplerFT += dt;

            if (_timerSamplerFPS >= FPS_SAMPLE_RATE)
            {
                FPS_SAMPLE = (int)FPS;
                _timerSamplerFPS = 0;
            }
            if (_timerSamplerFT >= FT_SAMPLE_RATE)
            {
                FT_SAMPLE = (int)FrameTime;
                _timerSamplerFT = 0;
            }
        }
    }

}
