using System.Numerics;
using System.Runtime.InteropServices;

namespace Phoenix.Framework.Rendering.Geometry.Indirect;

/// <summary>
/// GPU metadata for an individual indirect submesh draw command.
/// Matches the std430 storage buffer layout read by indirect vertex and fragment shaders.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct SubmeshMetadata
{
    public Matrix4x4 World;
    public Matrix4x4 WorldInverseTranspose;
    public uint AlbedoIndex;
    public uint NormalIndex;
    public uint SpecularIndex;
    public uint MaterialFlags;

    public SubmeshMetadata(
        in Matrix4x4 world,
        uint albedoIndex = 0,
        uint normalIndex = 0,
        uint specularIndex = 0,
        uint materialFlags = 0)
    {
        World = world;
        if (Matrix4x4.Invert(world, out var inv))
        {
            WorldInverseTranspose = Matrix4x4.Transpose(inv);
        }
        else
        {
            WorldInverseTranspose = Matrix4x4.Identity;
        }

        AlbedoIndex = albedoIndex;
        NormalIndex = normalIndex;
        SpecularIndex = specularIndex;
        MaterialFlags = materialFlags;
    }
}
