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
            if (_timerSamplerUPS >= UPS_SAMPLE_RATE && dt > 0)
            {
                UPS_SAMPLE = (int)(1.0 / UPS);
                _timerSamplerUPS = 0;
            }
        }
        internal void ProcessRender(double dt)
        {
            FrameTime = dt;

            if (dt > 0)
                FPS = 1.0 / dt;
            _timerSamplerFPS += dt;
            _timerSamplerFT += dt;

            if (_timerSamplerFPS >= FPS_SAMPLE_RATE && dt > 0)
            {
                FPS_SAMPLE = (int)FPS;
                _timerSamplerFPS = 0;
            }
            if (_timerSamplerFT >= FT_SAMPLE_RATE && dt > 0)
            {
                FT_SAMPLE = (int)(FrameTime * 1000.0);
                _timerSamplerFT = 0;
            }
        }
    }

}
