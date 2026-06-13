using Phoenix.Framework.Rendering.Geometry.Model;
using Phoenix.Framework.Rendering.Geometry.Model.Meshes;
using Phoenix.Framework.Rendering.Geometry.Vertices;
using Silk.NET.GLFW;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Phoenix.Framework.Rendering.Primitives;

internal static class PrimitiveHelper
{
    private static Dictionary<InfoCube, Cube> _primitivesCube = new();
    private static Dictionary<InfoSphere, Sphere> _primitivesSphere = new();
    private static Dictionary<InfoPlane, Plane> _primitivesPlane = new();
    private static Dictionary<InfoCylinder, Cylinder> _primitivesCylinder = new();
    private static Dictionary<InfoPyramid, Pyramid> _primitivesPyramid = new();
    private static Dictionary<InfoFrustum, Frustum> _primitivesFrustum = new();

    internal static Cube Cube(InfoCube info)
    {
        if(!_primitivesCube.TryGetValue(info, out var cube))
        {
            cube = new Cube(info);
            _primitivesCube.Add(info, cube);
        }

        return cube;
    }

    internal static Cube Cube()
    {
        return Cube(new InfoCube{
            MeshPrimitiveType = PrimitiveType.Triangles,
            Uv = true,
            Normals = true
            }); 
    }

    internal static Sphere Sphere(InfoSphere info)
    {
        if(!_primitivesSphere.TryGetValue(info, out var sphere))
        {
            sphere = new Sphere(info);
            _primitivesSphere.Add(info, sphere);
        }

        return sphere;
    }

    internal static Sphere Sphere()
    {
        return Sphere(new InfoSphere{
            SubDivisions = 16,
            MeshPrimitiveType = PrimitiveType.Triangles,
            Uv = true,
            Normals = true
            }); 
    }

    internal static Plane Plane(InfoPlane info)
    {
        if(!_primitivesPlane.TryGetValue(info, out var plane))
        {
            plane = new Plane(info);
            _primitivesPlane.Add(info, plane);
        }

        return plane;
    }

    internal static Plane Plane()
    {
        return Plane(new InfoPlane{
            MeshPrimitiveType = PrimitiveType.Triangles,
            Uv = true,
            Normals = true
            }); 
    }

    internal static Cylinder Cylinder(InfoCylinder info)
    {
        if(!_primitivesCylinder.TryGetValue(info, out var cylinder))
        {
            cylinder = new Cylinder(info);
            _primitivesCylinder.Add(info, cylinder);
        }

        return cylinder;
    }

    internal static Cylinder Cylinder()
    {
        return Cylinder(new InfoCylinder{
            SubDivisions = 16,
            MeshPrimitiveType = PrimitiveType.Triangles,
            Uv = true,
            Normals = true
            }); 
    }

    internal static Pyramid Pyramid(InfoPyramid info)
    {
        if(!_primitivesPyramid.TryGetValue(info, out var pyramid))
        {
            pyramid = new Pyramid(info);
            _primitivesPyramid.Add(info, pyramid);
        }

        return pyramid;
    }

    internal static Pyramid Pyramid()
    {
        return Pyramid(new InfoPyramid{
            MeshPrimitiveType = PrimitiveType.Triangles,
            Uv = true,
            Normals = true
            }); 
    }

    internal static Frustum Frustum(InfoFrustum info)
    {
        if(!_primitivesFrustum.TryGetValue(info, out var frustum))
        {
            frustum = new Frustum(info);
            _primitivesFrustum.Add(info, frustum);
        }

        return frustum;
    }

    internal static Frustum Frustum()
    {
        return Frustum(new InfoFrustum{
            NearWidth = 1.0f,
            NearHeight = 1.0f,
            FarWidth = 2.0f,
            FarHeight = 2.0f,
            Depth = 1.0f,
            MeshPrimitiveType = PrimitiveType.Triangles,
            Uv = true,
            Normals = true
            }); 
    }
    
}