using ImGuiNET;
using Phoenix.Framework.AssetImport;
using Phoenix.Framework.AssetImport.Processing;
using Phoenix.Framework.Rendering.Vulkan;
using Silk.NET.Input;
using System.Numerics;

namespace Phoenix.Framework.Rendering.GUI;

/// <summary>
/// Immediate-mode 2D graphical user interface facade coordinating text, shapes, buttons, and debug overlays.
/// </summary>
public sealed class UI : IDisposable
{
    private readonly PhoenixGame _game;
    private readonly ImGuiController _controller;
    private readonly Dictionary<int, ImFontPtr> _fonts = [];
    private VulkanTexture? _fontTexture;
    private int _buttonId;
    private int _fontPushCount;
    private bool _disposed;

    public ImGuiController Controller => _controller;

    /// <summary>
    /// Initializes UI controller, font atlas, and centralized error reporting.
    /// </summary>
    internal UI(PhoenixGame game)
    {
        _game = game;
        _controller = new ImGuiController(game);

        LoadDefaultFont();
        ErrorListWindow.SetUI(this);
    }

    /// <summary>
    /// Loads the default embedded Cascadia Mono font atlas in sizes from 10 to 100.
    /// </summary>
    public void LoadDefaultFont()
    {
        List<int> sizes = new(91);
        for (int i = 10; i <= 100; i++)
        {
            sizes.Add(i);
        }

        string fontPath = EmbeddedHelper.ExtractPath("CascadiaMono.ttf", "Files.Fonts");
        LoadFontTTF(fontPath, sizes.ToArray());
    }

    /// <summary>
    /// Loads a TrueType font file at the specified pixel sizes and uploads the font atlas to Vulkan.
    /// </summary>
    public unsafe void LoadFontTTF(string path, int[] sizes)
    {
        if (sizes.Length == 0)
        {
            ErrorListWindow.Add("Font loading requires at least one font size.");
            return;
        }

        var io = ImGui.GetIO();
        io.Fonts.Clear();
        _fonts.Clear();

        foreach (int size in sizes)
        {
            _fonts[size] = io.Fonts.AddFontFromFileTTF(path, size);
        }

        io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height, out int bytesPerPixel);

        var fontOptions = new TextureLoadOptions
        {
            GenerateMipmaps = false,
            Compression = TextureCompressionFormat.None
        };

        ReadOnlySpan<byte> pixelSpan = new(pixels, width * height * 4);
        var newFontTexture = _game.Graphics.CreateTexture(width, height, pixelSpan, fontOptions);

        io.Fonts.SetTexID((nint)newFontTexture.TextureId);
        io.Fonts.ClearTexData();

        _fontTexture?.Dispose();
        _fontTexture = newFontTexture;

        if (_fonts.Count > 0)
        {
            _currentFontSize = _fonts.Keys.Min();
        }
    }

    private int _currentFontSize = 16;

    /// <summary>
    /// Selects an active font size for subsequent immediate-mode text rendering.
    /// </summary>
    public void SetFontSize(int size)
    {
        _currentFontSize = size;
        if (!_controller.IsFrameActive)
            return;

        if (!_fonts.TryGetValue(size, out var font))
        {
            ErrorListWindow.Add($"Font size {size} not loaded.");
            return;
        }

        while (_fontPushCount > 0)
        {
            ImGui.PopFont();
            _fontPushCount--;
        }

        ImGui.PushFont(font);
        _fontPushCount++;
    }

    /// <summary>
    /// Draws unadorned text directly to the screen viewport.
    /// </summary>
    public void DrawText(string text, Vector2 position, Vector4 color, int size = 16)
    {
        if (!_controller.IsFrameActive)
            return;

        var drawList = ImGui.GetForegroundDrawList();
        bool pushed = _fonts.TryGetValue(size, out var font);
        if (pushed)
            ImGui.PushFont(font);

        drawList.AddText(position, ImGui.ColorConvertFloat4ToU32(color), text);

        if (pushed)
            ImGui.PopFont();
    }

    /// <summary>
    /// Draws centered text horizontally and vertically around the specified anchor.
    /// </summary>
    public void DrawCenteredText(string text, Vector2 position, Vector4 color, int size = 16)
    {
        if (!_controller.IsFrameActive)
            return;

        var drawList = ImGui.GetForegroundDrawList();
        bool pushed = _fonts.TryGetValue(size, out var font);
        if (pushed)
            ImGui.PushFont(font);

        var textSize = ImGui.CalcTextSize(text);
        drawList.AddText(position - textSize / 2f, ImGui.ColorConvertFloat4ToU32(color), text);

        if (pushed)
            ImGui.PopFont();
    }

    /// <summary>
    /// Draws horizontally centered text anchored at the specified position.
    /// </summary>
    public void DrawHCenteredText(string text, Vector2 position, Vector4 color, int size = 16)
    {
        if (!_controller.IsFrameActive)
            return;

        var drawList = ImGui.GetForegroundDrawList();
        bool pushed = _fonts.TryGetValue(size, out var font);
        if (pushed)
            ImGui.PushFont(font);

        var textSize = ImGui.CalcTextSize(text);
        drawList.AddText(new Vector2(position.X - textSize.X / 2f, position.Y), ImGui.ColorConvertFloat4ToU32(color), text);

        if (pushed)
            ImGui.PopFont();
    }

    /// <summary>
    /// Draws right-aligned text ending at the specified x coordinate.
    /// </summary>
    public void DrawRAlignedText(string text, Vector2 position, Vector4 color, int size = 16)
    {
        if (!_controller.IsFrameActive)
            return;

        var drawList = ImGui.GetForegroundDrawList();
        bool pushed = _fonts.TryGetValue(size, out var font);
        if (pushed)
            ImGui.PushFont(font);

        var textSize = ImGui.CalcTextSize(text);
        drawList.AddText(new Vector2(position.X - textSize.X, position.Y), ImGui.ColorConvertFloat4ToU32(color), text);

        if (pushed)
            ImGui.PopFont();
    }

    /// <summary>
    /// Draws a texture image by asset path.
    /// </summary>
    public void DrawImg(string name, Vector2 position, Vector2 size)
    {
        DrawImg(name, position, size, Vector2.Zero, Vector2.One);
    }

    /// <summary>
    /// Draws a cropped texture image by asset path with explicit UV coordinates.
    /// </summary>
    public void DrawImg(string name, Vector2 position, Vector2 size, Vector2 uvMin, Vector2 uvMax)
    {
        try
        {
            var tex = AssetLoader.LoadTexture(name);
            DrawImg(tex.TextureId, position, size, uvMin, uvMax);
        }
        catch (Exception e)
        {
            ErrorListWindow.Add($"Could not draw texture '{name}': {e.Message}");
        }
    }

    /// <summary>
    /// Draws a texture image from an allocated VulkanTexture handle.
    /// </summary>
    public void DrawImg(VulkanTexture tex, Vector2 position, Vector2 size)
    {
        DrawImg(tex.TextureId, position, size, Vector2.Zero, Vector2.One);
    }

    /// <summary>
    /// Draws a cropped texture image from an allocated VulkanTexture handle with explicit UV coordinates.
    /// </summary>
    public void DrawImg(VulkanTexture tex, Vector2 position, Vector2 size, Vector2 uvMin, Vector2 uvMax)
    {
        DrawImg(tex.TextureId, position, size, uvMin, uvMax);
    }

    /// <summary>
    /// Draws an image from a bindless texture slot with pixel source and destination rectangles.
    /// </summary>
    public void DrawImg(uint texId, Vector2 texSize, Vector2 srcPosition, Vector2 srcSize, Vector2 dstPosition, Vector2 dstSize)
    {
        if (texSize.X == 0 || texSize.Y == 0)
            return;

        var uvMin = srcPosition / texSize;
        var uvMax = (srcPosition + srcSize) / texSize;
        DrawImg(texId, dstPosition, dstSize, uvMin, uvMax);
    }

    /// <summary>
    /// Draws a texture image using its bindless texture slot and normalized UV bounds.
    /// </summary>
    public void DrawImg(uint texId, Vector2 position, Vector2 size, Vector2 uvMin, Vector2 uvMax)
    {
        var drawList = ImGui.GetForegroundDrawList();
        drawList.AddImage((nint)texId, position, position + size, uvMin, uvMax);
    }

    /// <summary>
    /// Draws a simple auto-sized interactive button invoking the provided callback when clicked.
    /// </summary>
    public void DrawSimpleButton(string name, Vector2 position, Vector2 size, Action action)
    {
        string guiName = $"btn_{_buttonId++}";
        ImGui.SetNextWindowPos(position);

        if (ImGui.Begin(guiName, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse))
        {
            if (ImGui.Button(name))
            {
                action();
            }
        }
        ImGui.End();
    }

    /// <summary>
    /// Updates UI timing, input states, and diagnostics overlays.
    /// </summary>
    internal void Update(double delta)
    {
        _buttonId = 0;

        while (_fontPushCount > 0)
        {
            ImGui.PopFont();
            _fontPushCount--;
        }

        if (_game.Input.KeyDownOnce(Key.F10))
        {
            AssetLoadingDebugWindow.Show = !AssetLoadingDebugWindow.Show;
        }

        _controller.Update((float)delta);
        ErrorListWindow.Update((float)delta);
        AssetLoadingDebugWindow.Update((float)delta);
    }

    /// <summary>
    /// Renders overlays and submits UI geometry to the Vulkan command stream.
    /// </summary>
    internal void Render(RenderContext ctx)
    {
        ErrorListWindow.Render();
        AssetLoadingDebugWindow.Render();

        while (_fontPushCount > 0)
        {
            ImGui.PopFont();
            _fontPushCount--;
        }

        _controller.Render(ctx);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _fontTexture?.Dispose();
        _controller.Dispose();
    }
}
