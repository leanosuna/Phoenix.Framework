using ImGuiNET;
using Phoenix.Framework.Rendering.Vulkan;
using Silk.NET.Input;
using Silk.NET.Windowing;
using System.Numerics;

namespace Phoenix.Framework.Rendering.GUI;

/// <summary>
/// Bridges Silk.NET input and window events to ImGui and coordinates frame lifecycle.
/// </summary>
public sealed class ImGuiController : IDisposable
{
    private readonly PhoenixGame _game;
    private readonly IWindow _window;
    private readonly IInputContext _inputContext;
    private readonly ImGuiRenderer _renderer;
    private readonly nint _imguiContext;
    private bool _frameActive;
    private bool _disposed;

    public ImGuiRenderer Renderer => _renderer;
    public bool IsFrameActive => _frameActive;

    /// <summary>
    /// Initializes ImGui context, event listeners, and the Vulkan renderer backend.
    /// </summary>
    public ImGuiController(PhoenixGame game)
    {
        _game = game;
        _window = game.Window;
        _inputContext = game.Input.GetContext();

        _imguiContext = ImGui.CreateContext();
        ImGui.SetCurrentContext(_imguiContext);

        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

        ImGui.StyleColorsDark();

        _renderer = new ImGuiRenderer(game.Graphics);

        HookInputEvents();
    }

    private void HookInputEvents()
    {
        for (int i = 0; i < _inputContext.Keyboards.Count; i++)
        {
            var kb = _inputContext.Keyboards[i];
            kb.KeyChar += OnKeyChar;
            kb.KeyDown += OnKeyDown;
            kb.KeyUp += OnKeyUp;
        }

        for (int i = 0; i < _inputContext.Mice.Count; i++)
        {
            var mouse = _inputContext.Mice[i];
            mouse.MouseMove += OnMouseMove;
            mouse.MouseDown += OnMouseDown;
            mouse.MouseUp += OnMouseUp;
            mouse.Scroll += OnMouseScroll;
        }
    }

    private void UnhookInputEvents()
    {
        for (int i = 0; i < _inputContext.Keyboards.Count; i++)
        {
            var kb = _inputContext.Keyboards[i];
            kb.KeyChar -= OnKeyChar;
            kb.KeyDown -= OnKeyDown;
            kb.KeyUp -= OnKeyUp;
        }

        for (int i = 0; i < _inputContext.Mice.Count; i++)
        {
            var mouse = _inputContext.Mice[i];
            mouse.MouseMove -= OnMouseMove;
            mouse.MouseDown -= OnMouseDown;
            mouse.MouseUp -= OnMouseUp;
            mouse.Scroll -= OnMouseScroll;
        }
    }

    private void OnKeyChar(IKeyboard keyboard, char c)
    {
        ImGui.GetIO().AddInputCharacter(c);
    }

    private void OnKeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        ImGuiKey imKey = TranslateKey(key);
        if (imKey != ImGuiKey.None)
            ImGui.GetIO().AddKeyEvent(imKey, true);
    }

    private void OnKeyUp(IKeyboard keyboard, Key key, int keyCode)
    {
        ImGuiKey imKey = TranslateKey(key);
        if (imKey != ImGuiKey.None)
            ImGui.GetIO().AddKeyEvent(imKey, false);
    }

    private void OnMouseMove(IMouse mouse, Vector2 pos)
    {
        ImGui.GetIO().AddMousePosEvent(pos.X, pos.Y);
    }

    private void OnMouseDown(IMouse mouse, MouseButton button)
    {
        ImGui.GetIO().AddMouseButtonEvent((int)button, true);
    }

    private void OnMouseUp(IMouse mouse, MouseButton button)
    {
        ImGui.GetIO().AddMouseButtonEvent((int)button, false);
    }

    private void OnMouseScroll(IMouse mouse, ScrollWheel wheel)
    {
        ImGui.GetIO().AddMouseWheelEvent(wheel.X, wheel.Y);
    }

    /// <summary>
    /// Begins a new ImGui frame with current display dimensions and delta time.
    /// </summary>
    public void Update(float deltaTime)
    {
        if (_frameActive)
            ImGui.Render();

        var io = ImGui.GetIO();
        io.DeltaTime = Math.Max(deltaTime, 0.00001f);
        io.DisplaySize = new Vector2(_window.Size.X, _window.Size.Y);

        if (_window.Size.X > 0 && _window.Size.Y > 0)
        {
            io.DisplayFramebufferScale = new Vector2(
                (float)_window.FramebufferSize.X / _window.Size.X,
                (float)_window.FramebufferSize.Y / _window.Size.Y
            );
        }

        ImGui.NewFrame();
        _frameActive = true;
    }

    /// <summary>
    /// Finalizes the ImGui frame and records draw calls into the active UI dynamic render pass.
    /// </summary>
    public void Render(RenderContext ctx)
    {
        if (!_frameActive)
            return;

        _frameActive = false;
        ImGui.Render();

        var drawData = ImGui.GetDrawData();
        if (drawData.CmdListsCount > 0)
        {
            using (ctx.BeginUIPass())
            {
                _renderer.Render(ctx, drawData, _game.Graphics.Swapchain.Extent);
            }
        }
    }

    private static ImGuiKey TranslateKey(Key key) => key switch
    {
        Key.Tab => ImGuiKey.Tab,
        Key.Left => ImGuiKey.LeftArrow,
        Key.Right => ImGuiKey.RightArrow,
        Key.Up => ImGuiKey.UpArrow,
        Key.Down => ImGuiKey.DownArrow,
        Key.PageUp => ImGuiKey.PageUp,
        Key.PageDown => ImGuiKey.PageDown,
        Key.Home => ImGuiKey.Home,
        Key.End => ImGuiKey.End,
        Key.Insert => ImGuiKey.Insert,
        Key.Delete => ImGuiKey.Delete,
        Key.Backspace => ImGuiKey.Backspace,
        Key.Space => ImGuiKey.Space,
        Key.Enter => ImGuiKey.Enter,
        Key.Escape => ImGuiKey.Escape,
        Key.ControlLeft => ImGuiKey.LeftCtrl,
        Key.ControlRight => ImGuiKey.RightCtrl,
        Key.ShiftLeft => ImGuiKey.LeftShift,
        Key.ShiftRight => ImGuiKey.RightShift,
        Key.AltLeft => ImGuiKey.LeftAlt,
        Key.AltRight => ImGuiKey.RightAlt,
        Key.SuperLeft => ImGuiKey.LeftSuper,
        Key.SuperRight => ImGuiKey.RightSuper,
        Key.Menu => ImGuiKey.Menu,
        Key.Number0 => ImGuiKey._0,
        Key.Number1 => ImGuiKey._1,
        Key.Number2 => ImGuiKey._2,
        Key.Number3 => ImGuiKey._3,
        Key.Number4 => ImGuiKey._4,
        Key.Number5 => ImGuiKey._5,
        Key.Number6 => ImGuiKey._6,
        Key.Number7 => ImGuiKey._7,
        Key.Number8 => ImGuiKey._8,
        Key.Number9 => ImGuiKey._9,
        Key.A => ImGuiKey.A,
        Key.B => ImGuiKey.B,
        Key.C => ImGuiKey.C,
        Key.D => ImGuiKey.D,
        Key.E => ImGuiKey.E,
        Key.F => ImGuiKey.F,
        Key.G => ImGuiKey.G,
        Key.H => ImGuiKey.H,
        Key.I => ImGuiKey.I,
        Key.J => ImGuiKey.J,
        Key.K => ImGuiKey.K,
        Key.L => ImGuiKey.L,
        Key.M => ImGuiKey.M,
        Key.N => ImGuiKey.N,
        Key.O => ImGuiKey.O,
        Key.P => ImGuiKey.P,
        Key.Q => ImGuiKey.Q,
        Key.R => ImGuiKey.R,
        Key.S => ImGuiKey.S,
        Key.T => ImGuiKey.T,
        Key.U => ImGuiKey.U,
        Key.V => ImGuiKey.V,
        Key.W => ImGuiKey.W,
        Key.X => ImGuiKey.X,
        Key.Y => ImGuiKey.Y,
        Key.Z => ImGuiKey.Z,
        Key.F1 => ImGuiKey.F1,
        Key.F2 => ImGuiKey.F2,
        Key.F3 => ImGuiKey.F3,
        Key.F4 => ImGuiKey.F4,
        Key.F5 => ImGuiKey.F5,
        Key.F6 => ImGuiKey.F6,
        Key.F7 => ImGuiKey.F7,
        Key.F8 => ImGuiKey.F8,
        Key.F9 => ImGuiKey.F9,
        Key.F10 => ImGuiKey.F10,
        Key.F11 => ImGuiKey.F11,
        Key.F12 => ImGuiKey.F12,
        _ => ImGuiKey.None
    };

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        UnhookInputEvents();
        _renderer.Dispose();

        if (_imguiContext != nint.Zero)
            ImGui.DestroyContext(_imguiContext);
    }
}
