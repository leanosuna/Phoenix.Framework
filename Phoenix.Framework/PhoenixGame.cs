using Phoenix;
using Phoenix.Framework.Cameras;
using Phoenix.Framework.Inputs;
using Phoenix.Framework.Maths;
using Phoenix.Framework.Rendering;
using Phoenix.Framework.Rendering.Vulkan;
using Phoenix.Framework.Rendering.Windowing;
using Phoenix.Framework.Sound;
using Silk.NET.Core;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Numerics;

namespace Phoenix.Framework;

public abstract class PhoenixGame : IDisposable
{
    /// <summary>
    /// Gets the currently configured windowing platform backend.
    /// </summary>
    public static WindowBackend ConfiguredPlatform => BackendHelper.ConfiguredPlatform;

    /// <summary>
    /// Explicitly selects the windowing backend before window initialization on Linux.
    /// </summary>
    public static void SetPlatform(WindowBackend platform) => BackendHelper.SetPlatform(platform);

    /// <summary>
    /// Inspects command-line arguments and environment variables to configure the windowing platform.
    /// </summary>
    public static void ConfigurePlatform(string[]? args = null) => BackendHelper.ConfigurePlatform(args);

    public IWindow Window { get; private set; }
    public Vector2 WindowSize { get; private set; }
    public Vector2 FramebufferSize { get; private set; }
    public int WindowWidth => (int)WindowSize.X;
    public int WindowHeight => (int)WindowSize.Y;
    public int FramebufferWidth => (int)FramebufferSize.X;
    public int FramebufferHeight => (int)FramebufferSize.Y;

    public Input Input { get; private set; } = default!;
    public Camera? Camera { get; set; }
    public Metrics Metrics { get; } = new Metrics();

    /// <summary>
    /// Gets the graphics engine managing Vulkan devices, swapchain, uniform buffers, and pipelines.
    /// </summary>
    public Graphics Graphics { get; private set; } = default!;

    /// <summary>
    /// Gets the parsed command-line launch arguments.
    /// </summary>
    public GameArguments Arguments { get; }

    /// <summary>
    /// Constructs a PhoenixGame with the provided command-line arguments and default window settings.
    /// </summary>
    protected PhoenixGame(string[]? args)
        : this(args, null)
    {
    }

    /// <summary>
    /// Constructs a PhoenixGame with command-line arguments and custom base window configuration.
    /// </summary>
    protected PhoenixGame(string[]? args, WindowOptions? options)
    {
        Arguments = GameArguments.Parse(args);
        BackendHelper.SetPlatform(Arguments.WindowBackend);

        var winOptions = WindowOptions.Default;
        winOptions.API = GraphicsAPI.None;

        if(options is null)
            ApplyDefaultWindowSettings(ref winOptions);
        
        ConfigureWindow(ref winOptions);

        Window = Silk.NET.Windowing.Window.Create(winOptions);
        WindowSize = Window.Size.ToNum();
        FramebufferSize = WindowSize;

        Window.Load += InternalLoad;
        Window.Update += InternalUpdate;
        Window.Render += InternalRender;
        Window.FramebufferResize += InternalFramebufferResize;
        Window.Closing += InternalOnClose;
    }

    /// <summary>
    /// Applies framework command-line arguments to WindowOptions before window creation.
    /// </summary>
    private void ApplyDefaultWindowSettings(ref WindowOptions winOptions)
    {
        if (Arguments is { Width: not null, Height: not null })
            winOptions.Size = new Vector2D<int>(Arguments.Width.Value, Arguments.Height.Value);
        else if (winOptions.Size.X <= 0 || winOptions.Size.Y <= 0)
            winOptions.Size = new Vector2D<int>(1600, 900);

        if (Arguments.VSync.HasValue)
            winOptions.VSync = Arguments.VSync.Value;

        if (Arguments.Fullscreen.HasValue)
            winOptions.WindowState = Arguments.Fullscreen.Value ? WindowState.Fullscreen : WindowState.Normal;

        if (!string.IsNullOrEmpty(Arguments.Title))
            winOptions.Title = Arguments.Title;

        if (Arguments.FpsLimit.HasValue)
        {
            winOptions.FramesPerSecond = Arguments.FpsLimit.Value;
            winOptions.UpdatesPerSecond = Arguments.FpsLimit.Value;
        }
    }

    /// <summary>
    /// Invoked before window creation to allow derived games to inspect launch arguments and modify WindowOptions.
    /// Framework command-line argument defaults have already been applied to winOptions prior to this invocation.
    /// </summary>
    protected virtual void ConfigureWindow(ref WindowOptions winOptions)
    {
    }

    /// <summary>
    /// Starts the main game loop and runs the window.
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
            if (!string.IsNullOrEmpty(ex.StackTrace))
                strException += $"\n{ex.StackTrace}";
            Log.Exception(strException);
            throw;
        }

        Window.Dispose();
    }

    /// <summary>
    /// Closes and stops the game window.
    /// </summary>
    public void Stop()
    {
        Window.Close();
    }

    /// <summary>
    /// Disposes window and framework resources.
    /// </summary>
    public void Dispose()
    {
        Window.Dispose();
    }

    /// <summary>
    /// Invoked once after graphics and subsystems have loaded.
    /// </summary>
    protected abstract void Initialize();

    /// <summary>
    /// Invoked every frame to update game state.
    /// </summary>
    protected abstract void Update(double deltaTime);

    /// <summary>
    /// Invoked every frame to record rendering commands into the provided render context.
    /// </summary>
    protected abstract void Render(RenderContext ctx, double deltaTime);

    /// <summary>
    /// Invoked after scene rendering to draw user interface overlays.
    /// </summary>
    protected virtual void RenderUI()
    {
    }

    /// <summary>
    /// Invoked when the window or framebuffer size changes.
    /// </summary>
    protected virtual void OnWindowResize(Vector2 size)
    {
    }

    /// <summary>
    /// Invoked before the game shuts down.
    /// </summary>
    protected virtual void OnClose()
    {
    }

    /// <summary>
    /// Initializes window state, Vulkan backend, input, and viewport settings.
    /// </summary>
    private void InternalLoad()
    {
        Log.Enabled = true;
        Log.ConsoleWrite = true;
        Log.Info("Game starting (Vulkan 1.3 pipeline)");
        Window.Center();
        SetDefaultIcon();

        FramebufferSize = Window.FramebufferSize.ToNum();
        WindowSize = Window.Size.ToNum();

        Graphics = new Graphics(this);
        Input = new Input(this);

        InternalFramebufferResize(Window.FramebufferSize);

        SoundManager.Initialize();
        Initialize();

        if (Graphics.Swapchain.IsVSyncEnabled != Window.VSync)
        {
            Graphics.Swapchain.Recreate(Window.FramebufferSize);
        }
    }

    /// <summary>
    /// Coordinates internal update routines including input polling and metrics processing.
    /// </summary>
    private void InternalUpdate(double deltaTime)
    {
        Metrics.ProcessUpdate(deltaTime);
        Input.Update();

        if (Input.KeyDownOnce(Graphics.RenderHaltKey))
        {
            if (!Graphics.RenderHalt)
                Input.SetTemporaryMouseMode(Silk.NET.Input.CursorMode.Normal);
            else
                Input.RestoreMouseMode();

            Graphics.RenderHalt = !Graphics.RenderHalt;
        }

        if (!Graphics.RenderHalt)
            Update(deltaTime);
    }

    /// <summary>
    /// Coordinates frame rendering, uniform updates, dynamic passes, and swapchain presentation.
    /// </summary>
    private void InternalRender(double deltaTime)
    {
        Metrics.ProcessRender(deltaTime);

        if (!Graphics.Swapchain.AcquireNextImage(out uint imageIndex))
            return;

        Matrix4x4 view = Camera?.View ?? Matrix4x4.CreateLookAt(new Vector3(0, 0, 5), Vector3.Zero, Vector3.UnitY);
        Matrix4x4 proj = Camera?.Projection ?? Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4f, (float)FramebufferWidth / FramebufferHeight, 0.1f, 1000f);
        Vector3 camPos = Camera?.Position ?? new Vector3(0, 0, 5);

        CommonUBOData uboData = new(view, proj, camPos, (float)Metrics.Time, (float)deltaTime);
        Graphics.CommonUbo.Update(Graphics.Swapchain.CurrentFrame, in uboData);

        var cmd = Graphics.Swapchain.BeginCommandBuffer();
        Graphics.RenderContext.Prepare(cmd, imageIndex, Graphics.Swapchain.CurrentFrame);

        if (!Graphics.RenderHalt)
            Render(Graphics.RenderContext, deltaTime);

        if (!Graphics.RenderContext.HasRenderedPass)
            Graphics.RenderContext.FallbackClearPass(Graphics.ClearColor);

        RenderUI();

        Graphics.Swapchain.SubmitAndPresent(cmd, imageIndex);
    }

    /// <summary>
    /// Updates window dimension tracking and triggers swapchain recreation upon resize.
    /// </summary>
    private void InternalFramebufferResize(Vector2D<int> size)
    {
        FramebufferSize = new Vector2(size.X, size.Y);
        WindowSize = Window.Size.ToNum();

        Graphics.Resize(size);
        OnWindowResize(WindowSize);
    }

    /// <summary>
    /// Releases audio, Vulkan, and window resources on application close.
    /// </summary>
    private void InternalOnClose()
    {
        SoundManager.Shutdown();
        OnClose();

        Graphics.Dispose();
    }


    /// <summary>
    /// Extracts and assigns the default framework window icon.
    /// </summary>
    public void SetDefaultIcon()
    {
        SetIcon(EmbeddedHelper.ExtractPath("phnx.png", "Files.Icons"));
    }

    /// <summary>
    /// Assigns a custom window icon from a file path.
    /// </summary>
    public void SetCustomWindowIcon(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        SetIcon(path);
    }

    /// <summary>
    /// Loads an image from disk and passes the raw pixel buffer to the window icon API.
    /// </summary>
    private void SetIcon(string path)
    {
        using Image<Rgba32> image = Image.Load<Rgba32>(path);

        int w = image.Width;
        int h = image.Height;

        byte[] d = new byte[w * h * 4];
        image.CopyPixelDataTo(d);
        var img = new RawImage(w, h, (Memory<byte>)d);

        Window.SetWindowIcon(ref img);
    }
}
