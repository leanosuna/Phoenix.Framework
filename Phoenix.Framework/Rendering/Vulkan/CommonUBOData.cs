using System.Numerics;
using System.Runtime.InteropServices;

namespace Phoenix.Framework.Rendering.Vulkan;

/// <summary>
/// Uniform buffer data layout for camera matrices, position, and elapsed time sent to shaders at Set 0.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 16)]
public struct CommonUBOData
{
    public Matrix4x4 View;
    public Matrix4x4 Projection;
    public Vector3 CameraPosition;
    public float Time;
    public float DeltaTime;
    private Vector3 _padding;

    /// <summary>
    /// Initializes common uniform buffer data with camera transformations, world position, and elapsed timing values.
    /// </summary>
    public CommonUBOData(Matrix4x4 view, Matrix4x4 projection, Vector3 cameraPosition, float time, float deltaTime)
    {
        View = view;
        Projection = projection;
        CameraPosition = cameraPosition;
        Time = time;
        DeltaTime = deltaTime;
        _padding = Vector3.Zero;
    }
}
