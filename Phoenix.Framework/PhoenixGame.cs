using Phoenix;
using Phoenix.Framework.Cameras;
using Phoenix.Framework.Inputs;
using Phoenix.Framework.Maths;
using Phoenix.Framework.Rendering;
using Phoenix.Framework.Rendering.Vulkan;
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
    public IWindow Window { get; private set; }
    public Vector2 WindowSize { get; private set; }
    public Vector2 FramebufferSize { get; private set; }
    public int WindowWidth => (int)WindowSize.X;
    public int WindowHeight => (int)WindowSize.Y;
    public int FramebufferWidth => (int)FramebufferSize.X;
    public int FramebufferHeight => (int)FramebufferSize.Y;

    public Input Input { get; private set; } = default!;
    public Camera Camera { get; set; } = default!;
    public Metrics Metrics { get; } = new Metrics();
    public RenderViewport RenderViewport { get; private set; } = default!;

    public Silk.NET.Input.Key RenderHaltKey { get; set; } = Silk.NET.Input.Key.F11;
    public Vector4 ClearColor { get; set; } = new(0.1f, 0.12f, 0.16f, 1.0f);

    internal VulkanContext VulkanContext { get; private set; } = default!;
    internal VulkanSwapchain VulkanSwapchain { get; private set; } = default!;

    private bool _renderingHalt;

    /// <summary>
    /// Creates a PhoenixGame instance with default 1600x900 window options.
    /// </summary>
    public PhoenixGame()
    {
        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>(1600, 900);
        options.Title = "Phoenix Game (Vulkan)";
        options.VSync = true;
        options.API = GraphicsAPI.None;

        Window = Silk.NET.Windowing.Window.Create(options);
        WindowSize = Window.Size.ToNum();
        FramebufferSize = WindowSize;

        Window.Load += InternalLoad;
        Window.Update += InternalUpdate;
        Window.Render += InternalRender;
        Window.FramebufferResize += InternalFramebufferResize;
        Window.Closing += InternalOnClose;
    }

    /// <summary>
    /// Creates a PhoenixGame instance with custom window configuration options.
    /// </summary>
    public PhoenixGame(WindowOptions options)
    {
        options.API = GraphicsAPI.None;
        Window = Silk.NET.Windowing.Window.Create(options);

        WindowSize = Window.Size.ToNum();
        FramebufferSize = WindowSize;

        Window.Load += InternalLoad;
        Window.Update += InternalUpdate;
        Window.Render += InternalRender;
        Window.FramebufferResize += InternalFramebufferResize;
        Window.Closing += InternalOnClose;
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
        Window?.Dispose();
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
    /// Invoked every frame to record rendering commands.
    /// </summary>
    protected abstract void Render(double deltaTime);

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

        VulkanContext = new VulkanContext(Window, Window.Title);
        VulkanSwapchain = new VulkanSwapchain(VulkanContext, Window);

        Input = new Input(this);
        RenderViewport = new RenderViewport(this);

        InternalFramebufferResize(Window.FramebufferSize);

        SoundManager.Initialize();
        Initialize();
    }

    /// <summary>
    /// Coordinates internal update routines including input polling and metrics processing.
    /// </summary>
    private void InternalUpdate(double deltaTime)
    {
        Metrics.ProcessUpdate(deltaTime);
        Input.Update();

        if (Input.KeyDownOnce(RenderHaltKey))
        {
            if (!_renderingHalt)
                Input.SetTemporaryMouseMode(Silk.NET.Input.CursorMode.Normal);
            else
                Input.RestoreMouseMode();

            _renderingHalt = !_renderingHalt;
        }

        if (!_renderingHalt)
            Update(deltaTime);
    }

    /// <summary>
    /// Coordinates frame rendering, dynamic rendering pass execution, and swapchain presentation.
    /// </summary>
    private void InternalRender(double deltaTime)
    {
        Metrics.ProcessRender(deltaTime);

        if (!VulkanSwapchain.AcquireNextImage(out uint imageIndex))
            return;

        var cmd = VulkanSwapchain.BeginCommandBuffer();

        VulkanSwapchain.RecordClearPass(cmd, imageIndex, ClearColor);

        if (!_renderingHalt)
            Render(deltaTime);

        RenderUI();

        VulkanSwapchain.SubmitAndPresent(cmd, imageIndex);
    }

    /// <summary>
    /// Updates window dimension tracking and triggers swapchain recreation upon resize.
    /// </summary>
    private void InternalFramebufferResize(Vector2D<int> size)
    {
        FramebufferSize = new Vector2(size.X, size.Y);
        WindowSize = Window.Size.ToNum();

        VulkanSwapchain?.Recreate(size);
        OnWindowResize(WindowSize);
    }

    /// <summary>
    /// Releases audio, Vulkan, and window resources on application close.
    /// </summary>
    private void InternalOnClose()
    {
        SoundManager.Shutdown();
        OnClose();

        VulkanSwapchain?.Dispose();
        VulkanContext?.Dispose();
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
