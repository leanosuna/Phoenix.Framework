using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Phoenix.Cameras;
using Phoenix.Input;
using Phoenix.Network;
using Phoenix.Rendering;
using Phoenix.Rendering.Gizmos;
using Phoenix.Rendering.GUI;
using Phoenix.Rendering.RT;
using Phoenix.Rendering.Textures;
using Phoenix.Rendering.Shaders;
using Phoenix.Sound;
using Phoenix.AssetImport;

namespace Phoenix
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
        public RTManager RTManager { get; private set; } = default!;
        public Gizmos Gizmos { get; private set; } = default!;
        public GUIManager GUIManager { get; private set; } = default!;
        public NetworkManager NetworkManager { get; private set; } = default!;
        public Camera Camera { get; set; } = default!;
        public double Time { get; private set; } = 0;
        public double FrameTime { get; private set; } = 0;
        public double FT_SAMPLE { get; private set; } = 0;
        public double FT_SAMPLE_RATE { get; set; } = 0.3;
        public double FPS { get; private set; } = 0;
        public double FPS_SAMPLE { get; private set; } = 0;
        public double FPS_SAMPLE_RATE { get; set; } = 0.3;
        public uint CommonUboHandle { get; private set; } = 0;
        private CommonUBO _commonUboData;
        private bool _delayedLoadDone = false;

        public PhoenixGame()
        {
            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(1600, 900);
            options.Title = "Silk.NET OPENGL Game";
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
        /// This method gets called every time the window gets resized.
        /// </summary>
        /// <param name="windowSize">The new window size</param>
        protected abstract void OnWindowResize(Vector2D<int> windowSize);

        /// <summary>
        /// This method gets called when the game window is closed
        /// </summary>
        protected abstract void OnClose();

        private void InternalLoad()
        {
            Log.Enabled = true;
            Log.Info("Game starting");
            Window.Center();
            GL = GL.GetApi(Window);
            
            InputManager = new InputManager(this);
            GUIManager = new GUIManager(this);

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
            RTManager = new RTManager(this);
            Gizmos = new Gizmos(this);
            SoundManager.Initialize();
            //NetworkManager = new NetworkManager(this);
            AssetLoader.Init(this);

            // User defined Initialize
            Initialize();
            _delayedLoadDone = true;
        }
        bool _firstFrame = true;
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
            Time += deltaTime;

            InputManager.Update();

            //NetworkManager.Update();
            if (Gizmos.Enabled)
                Gizmos.Update();
            // User defined Update
            Update(deltaTime);

            UpdateCommonUBO(deltaTime);

            

        }

        private unsafe void UpdateCommonUBO(double dt)
        {
            _commonUboData = new CommonUBO(Camera.View, Camera.Projection, (float)Time, (float)dt);
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

            GUIManager.DrawCenteredText(str,new Vector2(WindowSize.X / 2, WindowSize.Y / 2), Vector4.One, 30);
            GUIManager.Render();
        }
        double _timerSamplerFPS = 0;
        double _timerSamplerFT = 0;

        private void InternalRender(double deltaTime)
        {
            FrameTime = deltaTime;
            
            FPS = 1.0 / deltaTime;
            _timerSamplerFPS += deltaTime;
            _timerSamplerFT += deltaTime;

            if (_timerSamplerFPS >= FPS_SAMPLE_RATE)
            {
                FPS_SAMPLE = FPS;
                _timerSamplerFPS = 0;
            }
            if (_timerSamplerFT >= FT_SAMPLE_RATE)
            {
                FT_SAMPLE = FrameTime;
                _timerSamplerFT = 0;
            }

            if (!_delayedLoadDone)
            {
                InitialLoadScreen();
                return;
            }
            GUIManager.Update(deltaTime);
            Render(deltaTime);
            if (Gizmos.Enabled)
                Gizmos.Render();
            GUIManager.Render();
        }

        public void SetResolution(Vector2 size, bool fullscreen)
        {
            if (fullscreen)
            {
                Window.WindowState = WindowState.Fullscreen;
                
            }
            else
            {
                Window.WindowState = WindowState.Normal;
                //Window.WindowBorder = 0;
                
            }
            Window.Position = Vector2D<int>.Zero;
            Window.Size = size.To2Di();
        }

        private void InternalFramebufferResize(Vector2D<int> size)
        {
            Console.WriteLine($"resize detected {size}");
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
