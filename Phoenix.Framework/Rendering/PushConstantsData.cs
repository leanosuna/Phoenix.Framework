using System.Numerics;
using System.Runtime.InteropServices;

namespace Phoenix.Framework.Rendering;

[StructLayout(LayoutKind.Sequential)]
public struct PushConstantsData
{
    public Matrix4x4 World;
    public uint AlbedoIndex;
    public uint NormalIndex;
    public uint MaterialIndex;
    public uint CustomFlags;

    /// <summary>
    /// Initializes push constants data with world transform and default indices.
    /// </summary>
    public PushConstantsData(Matrix4x4 world, uint albedoIndex = 0, uint normalIndex = 0, uint materialIndex = 0, uint customFlags = 0)
    {
        World = world;
        AlbedoIndex = albedoIndex;
        NormalIndex = normalIndex;
        MaterialIndex = materialIndex;
        CustomFlags = customFlags;
    }
}
