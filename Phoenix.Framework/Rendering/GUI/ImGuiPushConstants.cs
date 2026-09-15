using System.Numerics;
using System.Runtime.InteropServices;

namespace Phoenix.Framework.Rendering.GUI;

/// <summary>
/// Push constants payload uploaded to the Vulkan ImGui pipeline.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ImGuiPushConstants
{
    public Vector2 Scale;
    public Vector2 Translate;
    public uint TextureId;
}
