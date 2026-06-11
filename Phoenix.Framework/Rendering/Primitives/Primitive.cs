using Phoenix.Framework.Rendering.Geometry.Model;
using Phoenix.Framework.Rendering.Geometry.Model.Meshes;
using Phoenix.Framework.Rendering.Geometry.Vertices;
using Silk.NET.GLFW;
using Silk.NET.OpenGL;

namespace Phoenix.Framework.Rendering.Gizmos.Geometries.Primitives;

public abstract class Primitive
{
    protected Mesh _mesh;
    protected static GL GL;

    public static void SetGL(GL gl)
    {
        GL = gl;
    }
    public void Draw()
    {
        _mesh.Draw();
    }    

    public abstract void BuildMesh();  
    
}