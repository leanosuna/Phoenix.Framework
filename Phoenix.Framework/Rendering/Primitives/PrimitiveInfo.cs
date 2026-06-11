using System.Numerics;
using Silk.NET.OpenGL;

namespace Phoenix.Framework.Rendering.Gizmos.Geometries.Primitives;

public abstract class PrimitiveInfo
{
    public PrimitiveType MeshPrimitiveType = PrimitiveType.Triangles;
    public bool Uv = false;
    public bool Normals = false;
    public bool Tangents = false;
    public bool Bitangents = false;
    
    public Vector3 Size = Vector3.One;

    public bool FlipFaces = false;
    
    public PrimitiveInfo()
    {
        
    }

}