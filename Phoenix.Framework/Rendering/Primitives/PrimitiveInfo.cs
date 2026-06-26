using System.Numerics;
using Silk.NET.OpenGL;

namespace Phoenix.Framework.Rendering.Primitives;

public abstract class PrimitiveInfo
{
    public PrimitiveType MeshPrimitiveType = PrimitiveType.Triangles;
    public bool Uv = false;
    public bool Normals = false;
    public bool Tangents = false;
    public bool Bitangents = false;
    public bool SaveVertices = false;
    public PrimitiveInfo()
    {
        
    }

}