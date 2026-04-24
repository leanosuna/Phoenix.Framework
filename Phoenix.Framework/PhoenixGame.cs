using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Phoenix.Framework.Cameras;
using Phoenix.Framework.Input;
using Phoenix.Framework.Network;
using Phoenix.Framework.Rendering;
using Phoenix.Framework.Rendering.Gizmos;
using Phoenix.Framework.Rendering.GUI;
using Phoenix.Framework.Rendering.RT;
using Phoenix.Framework.Rendering.Shaders;
using Phoenix.Framework.Sound;
using Phoenix.Framework.AssetImport;
using System.Drawing;

namespace Phoenix.Framework
{
    public abstract class PhoenixGame
    {
        public GL GL { get; private set; } = default!;
        public IWindow Window { get; private set; }
        public Vector2 WindowSize { get; private set; }
        public int WindowWidth => (int)WindowSize.X;
        public int WindowHeight => (int)WindowSize.Y;
        public InputManager InputManager { get; private set; } = default!;
        public FullScreenQuad FullScreenQuad { get; private set; } = default!;
        public Gizmos Gizmos { get; private set; } = default!;
        public UI UI { get; private set; } = default!;
        public NetworkManager NetworkManager { get; private set; } = default!;
        public Camera Camera { get; set; } = default!;
        public Graphics Graphics { get; set; } = default!;
        
        public uint CommonUboHandle { get; private set; } = 0;

        internal RTManager RTManager = default!;

        private CommonUBO _commonUboData;
        private bool _delayedLoadDone = false;
        private bool _renderingHalt = false;
        private bool _firstFrame = true;

        internal RenderTarget _sceneRT;
        public PhoenixGame()
        {
            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(1600, 900);
            options.Title = "Phoenix Game";
            options.VSync = true;

            var glApi = new APIVersion(4, 1);
            options.API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.Default, glApi);

            Window = Silk.NET.Windowing.Window.Create(options);
            WindowSize = Window.Size.ToNum();

            Window.Load += InternalLoad;
            Window.Update += InternalUpdate;
            Window.Render += InternalRender;
            Window.FramebufferResize += InternalFramebufferResize;
            Window.Closing += InternalOnClose;
             

        }
        public PhoenixGame(WindowOptions options)
        {
            Window = Silk.NET.Windowing.Window.Create(options);

            WindowSize = Window.Size.ToNum();

            Window.Load += InternalLoad;
            Window.Update += InternalUpdate;
            Window.Render += InternalRender;
            Window.FramebufferResize += InternalFramebufferResize;
            Window.Closing += InternalOnClose;

        }
        /// <summary>
        /// Run the game (thread gets locked until game window is closed)
        /// </summary>
        public void Run()
        {
            try
            {
                Window.Run();
            }
            catch (Exception ex)
            {

                Log.Enabled = true;
                Log.Verbose = true;
                Log.Date = true;
                Log.Time = true;
                var strException = ex.Message;
                if (ex.StackTrace != null)
                    strException += $"\n{ex.StackTrace}";
                Log.Exception(strException);

                throw;
            }
            //thread blocked here until the window is closed.
            Window.Dispose();
        }
        /// <summary>
        /// Stop the game window
        /// </summary>
        public void Stop()
        {
            Window.Close();
        }
        /// <summary>
        /// This method gets called after GL Window and internal initialization
        /// </summary>
        protected abstract void Initialize();
        /// <summary>
        /// This method gets called every frame. Game logic should go here.
        /// </summary>
        /// <param name="deltaTime">Time in seconds since the last Update() call</param>
        protected abstract void Update(double deltaTime);
        /// <summary>
        /// This method gets called every frame. Game rendering should go here.
        /// </summary>
        /// <param name="deltaTime">Time in seconds since the last Render() call</param>
        protected abstract void Render(double deltaTime);

        /// <summary>
        /// This optional method gets called every frame. UI rendering should go here.
        /// </summary>
        /// <param name="deltaTime">Time in seconds since the last Render() call</param>

        protected virtual void RenderUI()
        {

        }

        /// <summary>
        /// This optional method gets called every time the window gets resized.
        /// </summary>
        /// <param name="windowSize">The new window size</param>
        protected virtual void OnWindowResize(Vector2D<int> windowSize)
        {

        }

        /// <summary>
        /// This optional method gets called when the game window is closed
        /// </summary>
        protected virtual void OnClose()
        {

        }

        private void InternalLoad()
        {
            Log.Info("Game starting");
            Window.Center();
            GL = GL.GetApi(Window);
            
            InputManager = new InputManager(this);
            UI = new UI(this);
            RTManager = new RTManager(this);
            _sceneRT = RTManager.BuildRT()
                .SetName("internal-scene")
                .AddTexture(RTManager.BuildRTT().Build())
                .SetDepthBuffer(new DepthBuffer())
                .Build();


            Graphics = new Graphics(this);

            GenCommonUBO();

        }
        private unsafe void GenCommonUBO()
        {
            CommonUboHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTargetARB.UniformBuffer, CommonUboHandle);
            GL.BufferData(BufferTargetARB.UniformBuffer, (nuint)(sizeof(CommonUBO)), null, BufferUsageARB.DynamicDraw);
            GL.BindBufferBase(BufferTargetARB.UniformBuffer, 0, CommonUboHandle);
        }

        private void DelayedLoad()
        {
            FullScreenQuad = new FullScreenQuad(this);
            Gizmos = new Gizmos(this);
            SoundManager.Initialize();
            //NetworkManager = new NetworkManager(this);
            AssetLoader.Init(this);

            // User defined Initialize
            Initialize();
            _delayedLoadDone = true;
        }
        
        private void InternalUpdate(double deltaTime)
        {
            if (!_delayedLoadDone)
            {
                if (!_firstFrame)
                {
                    DelayedLoad();
                }
                _firstFrame = false;
                return;
            }
            Graphics.Time += deltaTime;

            InputManager.Update();
            if(InputManager.KeyDownOnce(Graphics.RenderHaltKey))
            {
                if(!_renderingHalt)
                {
                    InputManager.SetTemporaryMouseMode(Silk.NET.Input.CursorMode.Normal);
                }
                else
                {
                    InputManager.RestoreMouseMode();
                }
                _renderingHalt = !_renderingHalt;

            }

            //NetworkManager.Update();
            // User defined Update

            if(!_renderingHalt)
            {
                if (Gizmos.Enabled)
                    Gizmos.Update();

                Update(deltaTime);
                UpdateCommonUBO(deltaTime);
            }
            
        }

        private unsafe void UpdateCommonUBO(double dt)
        {
            _commonUboData = new CommonUBO(Camera.View, Camera.Projection, (float)Graphics.Time, (float)dt);
            GL.BindBuffer(GLEnum.UniformBuffer, CommonUboHandle);
            fixed (void* d = & _commonUboData)
            {
                GL.BufferSubData(GLEnum.UniformBuffer, 0, (nuint)sizeof(CommonUBO), d);
            }
        }

        /// <summary>
        /// This allows you to show a first frame with a message, progress bar 
        /// or whatever you want while the Initialize() function runs
        /// </summary>
        protected virtual void InitialLoadScreen()
        {
            var str = "Loading game assets...";

            UI.DrawCenteredText(str,new Vector2(WindowSize.X / 2, WindowSize.Y / 2), Vector4.One, 30);
            UI.Render();
        }
        double _timerSamplerFPS = 0;
        double _timerSamplerFT = 0;

        private void InternalRender(double deltaTime)
        {
            Graphics.FrameTime = deltaTime;

            Graphics.FPS = 1.0 / deltaTime;
            _timerSamplerFPS += deltaTime;
            _timerSamplerFT += deltaTime;

            if (_timerSamplerFPS >= Graphics.FPS_SAMPLE_RATE)
            {
                Graphics.FPS_SAMPLE = Graphics.FPS;
                _timerSamplerFPS = 0;
            }
            if (_timerSamplerFT >= Graphics.FT_SAMPLE_RATE)
            {
                Graphics.FT_SAMPLE = Graphics.FrameTime;
                _timerSamplerFT = 0;
            }

            if (!_delayedLoadDone)
            {
                InitialLoadScreen();
                return;
            }
            UI.Update(deltaTime);

            
            if(!_renderingHalt)
            {
                Graphics.SetRenderToTarget(_sceneRT);

                Render(deltaTime);
                if (Gizmos.Enabled)
                    Gizmos.Render(); 
            }
            RTManager.TrueRenderToScreen();
            Graphics.ClearRenderTarget();

            var rv = Graphics.RenderViewport;
            Graphics.TrueCopyToScreen(_sceneRT, 0,
                new Vector4(0,0,rv.Width,rv.Height),
                new Vector4(0,0,WindowWidth, WindowHeight), rv.Filter);

            InternalRenderUI();
            RenderUI();

            UI.Render();
        }

        private void InternalRenderUI()
        {

        }
        
        

        private void InternalFramebufferResize(Vector2D<int> size)
        {
            WindowSize = new Vector2(size.X, size.Y);
            GL.Viewport(size);
            RTManager.HandleWindowResize();
            OnWindowResize(size);
        }
        private void InternalOnClose()
        {
            SoundManager.Shutdown();
            OnClose();
        }

        
        //public static void CheckGLError(string label)
        //{
        //    var err = GL.GetError();
        //    if (err != GLEnum.NoError)
        //        Log.Error($"[GL ERROR] {label}: {err}");
        //    //throw new Exception();
        //}

    }
}
