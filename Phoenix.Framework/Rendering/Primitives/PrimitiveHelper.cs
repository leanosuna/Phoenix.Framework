using Phoenix.Framework.Rendering.Geometry.Model;
using Phoenix.Framework.Rendering.Geometry.Model.Meshes;
using Phoenix.Framework.Rendering.Geometry.Vertices;
using Silk.NET.GLFW;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Phoenix.Framework.Rendering.Gizmos.Geometries.Primitives;

public static class PrimitiveHelper
{
    private static GL GL;

    private static Dictionary<InfoCube, Cube> _primitivesCube = new();
    private static Dictionary<InfoSphere, Sphere> _primitivesSphere = new();
    internal static void Init(GL gl)
    {
        GL = gl;
    }


    public static Cube Cube(InfoCube info)
    {
        if(!_primitivesCube.TryGetValue(info, out var cube))
        {
            cube = new Cube(info);
            _primitivesCube.Add(info, cube);
        }

        return cube;
    }

    public static Cube Cube()
    {
        return Cube(new InfoCube{
            MeshPrimitiveType = PrimitiveType.Triangles,
            Uv = true,
            Normals = true
            }); 
    }

    public static Sphere Sphere(InfoSphere info)
    {
        if(!_primitivesSphere.TryGetValue(info, out var sphere))
        {
            sphere = new Sphere(info);
            _primitivesSphere.Add(info, sphere);
        }

        return sphere;
    }

    public static Sphere Sphere()
    {
        return Sphere(new InfoSphere{
            SubDivisions = 16,
            MeshPrimitiveType = PrimitiveType.Triangles,
            Uv = true,
            Normals = true
            }); 
    }
    
}