using System.Numerics;
using System.Runtime.InteropServices;

namespace Phoenix.Framework.Rendering.Vulkan;

/// <summary>
/// Uniform buffer data layout for frame and camera parameters sent to shaders at descriptor Set 0.
/// Aligned to 16 bytes for std140 layout (352 bytes total).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 16)]
public struct FrameUniforms
{
    public Matrix4x4 View;
    public Matrix4x4 Projection;
    public Matrix4x4 ViewProjection;
    public Matrix4x4 InverseView;
    public Matrix4x4 InverseProjection;
    public Vector4 CameraPosition;
    public Vector2 Resolution;
    public float Time;
    public float DeltaTime;

    public FrameUniforms(Matrix4x4 view, Matrix4x4 projection, Vector3 cameraPosition, Vector2 resolution, float time, float deltaTime)
    {
        View = view;
        Projection = projection;
        ViewProjection = view * projection;
        Matrix4x4.Invert(view, out InverseView);
        Matrix4x4.Invert(projection, out InverseProjection);
        CameraPosition = new Vector4(cameraPosition, 1.0f);
        Resolution = resolution;
        Time = time;
        DeltaTime = deltaTime;
    }
}
