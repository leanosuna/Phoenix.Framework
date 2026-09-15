using Silk.NET.Vulkan;

namespace Phoenix.Framework.Rendering.Primitives;

internal static class PrimitiveHelper
{
    private static readonly Dictionary<InfoCube, Cube> _primitivesCube = [];
    private static readonly Dictionary<InfoSphere, Sphere> _primitivesSphere = [];
    private static readonly Dictionary<InfoPlane, Plane> _primitivesPlane = [];
    private static readonly Dictionary<InfoCylinder, Cylinder> _primitivesCylinder = [];
    private static readonly Dictionary<InfoPyramid, Pyramid> _primitivesPyramid = [];
    private static readonly Dictionary<InfoFrustum, Frustum> _primitivesFrustum = [];

    internal static Cube Cube(InfoCube info)
    {
        if (!_primitivesCube.TryGetValue(info, out var cube))
        {
            cube = new Cube(info);
            _primitivesCube.Add(info, cube);
        }

        return cube;
    }

    internal static Cube Cube()
    {
        return Cube(new InfoCube
        {
            MeshPrimitiveType = PrimitiveTopology.TriangleList,
            Uv = true,
            Normals = true
        });
    }

    internal static Sphere Sphere(InfoSphere info)
    {
        if (!_primitivesSphere.TryGetValue(info, out var sphere))
        {
            sphere = new Sphere(info);
            _primitivesSphere.Add(info, sphere);
        }

        return sphere;
    }

    internal static Sphere Sphere()
    {
        return Sphere(new InfoSphere
        {
            SubDivisions = 16,
            MeshPrimitiveType = PrimitiveTopology.TriangleList,
            Uv = true,
            Normals = true
        });
    }

    internal static Plane Plane(InfoPlane info)
    {
        if (!_primitivesPlane.TryGetValue(info, out var plane))
        {
            plane = new Plane(info);
            _primitivesPlane.Add(info, plane);
        }

        return plane;
    }

    internal static Plane Plane()
    {
        return Plane(new InfoPlane
        {
            MeshPrimitiveType = PrimitiveTopology.TriangleList,
            Uv = true,
            Normals = true
        });
    }

    internal static Cylinder Cylinder(InfoCylinder info)
    {
        if (!_primitivesCylinder.TryGetValue(info, out var cylinder))
        {
            cylinder = new Cylinder(info);
            _primitivesCylinder.Add(info, cylinder);
        }

        return cylinder;
    }

    internal static Cylinder Cylinder()
    {
        return Cylinder(new InfoCylinder
        {
            SubDivisions = 16,
            MeshPrimitiveType = PrimitiveTopology.TriangleList,
            Uv = true,
            Normals = true
        });
    }

    internal static Pyramid Pyramid(InfoPyramid info)
    {
        if (!_primitivesPyramid.TryGetValue(info, out var pyramid))
        {
            pyramid = new Pyramid(info);
            _primitivesPyramid.Add(info, pyramid);
        }

        return pyramid;
    }

    internal static Pyramid Pyramid()
    {
        return Pyramid(new InfoPyramid
        {
            MeshPrimitiveType = PrimitiveTopology.TriangleList,
            Uv = true,
            Normals = true
        });
    }

    internal static Frustum Frustum(InfoFrustum info)
    {
        if (!_primitivesFrustum.TryGetValue(info, out var frustum))
        {
            frustum = new Frustum(info);
            _primitivesFrustum.Add(info, frustum);
        }

        return frustum;
    }

    internal static Frustum Frustum()
    {
        return Frustum(new InfoFrustum
        {
            NearWidth = 1.0f,
            NearHeight = 1.0f,
            FarWidth = 2.0f,
            FarHeight = 2.0f,
            Depth = 1.0f,
            MeshPrimitiveType = PrimitiveTopology.TriangleList,
            Uv = true,
            Normals = true
        });
    }
}