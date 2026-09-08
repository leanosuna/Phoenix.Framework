using System.Runtime.InteropServices;

namespace Phoenix.Framework.Rendering.Windowing;

/// <summary>
/// Provides Linux windowing backend configuration and GLFW platform initialization hints.
/// </summary>
public static class BackendHelper
{
    private const int GlfwPlatformHint = 0x00050003;
    private const int GlfwPlatformWayland = 0x00060003;
    private const int GlfwPlatformX11 = 0x00060004;

    [DllImport("glfw", EntryPoint = "glfwInitHint")]
    private static extern void GlfwInitHint(int hint, int value);

    private static WindowBackend s_configuredPlatform = WindowBackend.Auto;
    private static bool s_platformConfigured;

    /// <summary>
    /// Gets the currently configured windowing platform backend.
    /// </summary>
    public static WindowBackend ConfiguredPlatform => s_configuredPlatform;

    /// <summary>
    /// Explicitly selects the windowing backend before window initialization on Linux.
    /// </summary>
    public static void SetPlatform(WindowBackend platform)
    {
        s_configuredPlatform = platform;
        s_platformConfigured = true;

        if (!OperatingSystem.IsLinux())
            return;

        try
        {
            switch (platform)
            {
                case WindowBackend.X11:
                    GlfwInitHint(GlfwPlatformHint, GlfwPlatformX11);
                    Log.Info("[Platform] Windowing platform set to X11");
                    break;
                case WindowBackend.Wayland:
                    GlfwInitHint(GlfwPlatformHint, GlfwPlatformWayland);
                    Log.Info("[Platform] Windowing platform set to Wayland");
                    break;
                case WindowBackend.Auto:
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"[Platform] Failed to set GLFW platform hint: {ex.Message}");
        }
    }

    /// <summary>
    /// Inspects command-line arguments and environment variables to configure the windowing platform.
    /// </summary>
    public static void ConfigurePlatform(string[]? args = null)
    {
        if (s_platformConfigured)
            return;

        var gameArgs = GameArguments.Parse(args);
        SetPlatform(gameArgs.WindowBackend);
    }
}
