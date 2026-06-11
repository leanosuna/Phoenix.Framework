using Phoenix.Framework.Rendering.Geometry.Model.Meshes;
using Phoenix.Framework.Rendering.Geometry.Vertices;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Phoenix.Framework.Rendering.Gizmos.Geometries.Primitives;

public class Sphere : Primitive
{
    public InfoSphere SphereInfo;

    public Sphere(InfoSphere sphereInfo) 
    {
        SphereInfo = sphereInfo;
        _primitiveInfo = sphereInfo;
        BuildMesh();
    }


    protected override void VertexIndexBufferPos(ref VertexBufferBuilder vbb, ref uint[] indices)
    {
        var lonSteps = Math.Max(SphereInfo.SubDivisions, 3);
        var latSteps = Math.Max(SphereInfo.SubDivisions / 2, 2);
        var indexList = new List<uint>();

        for (var lat = 0; lat <= latSteps; lat++)
        {
            var theta = MathF.PI * lat / latSteps - MathF.PI / 2f;
            var cosTheta = MathF.Cos(theta);

            for (var lon = 0; lon <= lonSteps; lon++)
            {
                var phi = 2f * MathF.PI * lon / lonSteps;
                vbb.Add(new Vector3(
                    cosTheta * MathF.Cos(phi),
                    MathF.Sin(theta),
                    cosTheta * MathF.Sin(phi)) * 0.5f);
            }
        }

        for (var lat = 0; lat <= latSteps; lat++)
        {
            var rowStart = (uint)(lat * (lonSteps + 1));
            for (var lon = 0; lon < lonSteps; lon++)
            {
                indexList.Add(rowStart + (uint)lon);
                indexList.Add(rowStart + (uint)(lon + 1));
            }
        }

        for (var lon = 0; lon <= lonSteps; lon++)
        {
            for (var lat = 0; lat < latSteps; lat++)
            {
                var cur = (uint)(lat * (lonSteps + 1) + lon);
                var next = (uint)((lat + 1) * (lonSteps + 1) + lon);
                indexList.Add(cur);
                indexList.Add(next);
            }
        }

        indices = indexList.ToArray();
    }

    protected override void VertexIndexBufferPosUv(ref VertexBufferBuilder vbb, ref uint[] indices)
    {
        var lonSteps = Math.Max(SphereInfo.SubDivisions, 3);
        var latSteps = Math.Max(SphereInfo.SubDivisions / 2, 2);
        var indexList = new List<uint>();

        for (var lat = 0; lat <= latSteps; lat++)
        {
            var theta = MathF.PI * lat / latSteps - MathF.PI / 2f;
            var sinTheta = MathF.Sin(theta);
            var cosTheta = MathF.Cos(theta);

            for (var lon = 0; lon <= lonSteps; lon++)
            {
                var phi = 2f * MathF.PI * lon / lonSteps;
                var x = cosTheta * MathF.Cos(phi);
                var y = sinTheta;
                var z = cosTheta * MathF.Sin(phi);

                vbb.Add(new Vector3(x, y, z) * 0.5f, new Vector2((float)lon / lonSteps, (float)lat / latSteps));
            }
        }

        for (var lat = 0; lat < latSteps; lat++)
        {
            for (var lon = 0; lon < lonSteps; lon++)
            {
                var first = (uint)(lat * (lonSteps + 1) + lon);
                var second = first + (uint)(lonSteps + 1);

                indexList.Add(first);
                indexList.Add(second);
                indexList.Add(first + 1);

                indexList.Add(second);
                indexList.Add(second + 1);
                indexList.Add(first + 1);
            }
        }

        indices = indexList.ToArray();
    }

    protected override void VertexIndexBufferPosUvNorm(ref VertexBufferBuilder vbb, ref uint[] indices)
    {
        var lonSteps = Math.Max(SphereInfo.SubDivisions, 3);
        var latSteps = Math.Max(SphereInfo.SubDivisions / 2, 2);
        var indexList = new List<uint>();

        for (var lat = 0; lat <= latSteps; lat++)
        {
            var theta = MathF.PI * lat / latSteps - MathF.PI / 2f;
            var sinTheta = MathF.Sin(theta);
            var cosTheta = MathF.Cos(theta);

            for (var lon = 0; lon <= lonSteps; lon++)
            {
                var phi = 2f * MathF.PI * lon / lonSteps;
                var x = cosTheta * MathF.Cos(phi);
                var y = sinTheta;
                var z = cosTheta * MathF.Sin(phi);
                var normal = new Vector3(x, y, z);

                vbb.Add(normal * 0.5f, new Vector2((float)lon / lonSteps, (float)lat / latSteps), normal);
            }
        }

        for (var lat = 0; lat < latSteps; lat++)
        {
            for (var lon = 0; lon < lonSteps; lon++)
            {
                var first = (uint)(lat * (lonSteps + 1) + lon);
                var second = first + (uint)(lonSteps + 1);

                indexList.Add(first);
                indexList.Add(second);
                indexList.Add(first + 1);

                indexList.Add(second);
                indexList.Add(second + 1);
                indexList.Add(first + 1);
            }
        }

        indices = indexList.ToArray();
    }

    protected override void VertexIndexBufferLines(ref VertexBufferBuilder vbb, ref uint[] indices)
    {
        var circleSegments = Math.Max(6, SphereInfo.SubDivisions);
        var indexList = new List<uint>();

        for (var i = 0; i < circleSegments; i++)
        {
            var angle = 2f * MathF.PI * i / circleSegments;
            vbb.Add(new Vector3(MathF.Cos(angle), MathF.Sin(angle), 0f) * 0.5f);
        }
        for (var i = 0; i < circleSegments; i++)
        {
            indexList.Add((uint)i);
            indexList.Add((uint)((i + 1) % circleSegments));
        }

        for (var i = 0; i < circleSegments; i++)
        {
            var angle = 2f * MathF.PI * i / circleSegments;
            vbb.Add(new Vector3(MathF.Cos(angle), 0f, MathF.Sin(angle)) * 0.5f);
        }
        var xzOffset = (uint)circleSegments;
        for (var i = 0; i < circleSegments; i++)
        {
            indexList.Add(xzOffset + (uint)i);
            indexList.Add(xzOffset + (uint)((i + 1) % circleSegments));
        }

        for (var i = 0; i < circleSegments; i++)
        {
            var angle = 2f * MathF.PI * i / circleSegments;
            vbb.Add(new Vector3(0f, MathF.Cos(angle), MathF.Sin(angle)) * 0.5f);
        }
        var yzOffset = 2 * (uint)circleSegments;
        for (var i = 0; i < circleSegments; i++)
        {
            indexList.Add(yzOffset + (uint)i);
            indexList.Add(yzOffset + (uint)((i + 1) % circleSegments));
        }

        indices = indexList.ToArray();
    }

    protected override void VertexIndexBufferPosUvNormTaBt(ref VertexBufferBuilder vbb, ref uint[] indices)
    {
        var lonSteps = Math.Max(SphereInfo.SubDivisions, 3);
        var latSteps = Math.Max(SphereInfo.SubDivisions / 2, 2);
        var indexList = new List<uint>();

        for (var lat = 0; lat <= latSteps; lat++)
        {
            var theta = MathF.PI * lat / latSteps - MathF.PI / 2f;
            var sinTheta = MathF.Sin(theta);
            var cosTheta = MathF.Cos(theta);

            for (var lon = 0; lon <= lonSteps; lon++)
            {
                var phi = 2f * MathF.PI * lon / lonSteps;
                var x = cosTheta * MathF.Cos(phi);
                var y = sinTheta;
                var z = cosTheta * MathF.Sin(phi);
                var normal = new Vector3(x, y, z);

                Vector3 tangent;
                if (MathF.Abs(cosTheta) > 1e-6f)
                    tangent = Vector3.Normalize(new Vector3(-MathF.Sin(phi), 0, MathF.Cos(phi)));
                else
                    tangent = new Vector3(1, 0, 0);

                var bitangent = Vector3.Normalize(Vector3.Cross(normal, tangent));

                vbb.Add(normal * 0.5f, new Vector2((float)lon / lonSteps, (float)lat / latSteps), normal, tangent, bitangent);
            }
        }

        for (var lat = 0; lat < latSteps; lat++)
        {
            for (var lon = 0; lon < lonSteps; lon++)
            {
                var first = (uint)(lat * (lonSteps + 1) + lon);
                var second = first + (uint)(lonSteps + 1);

                indexList.Add(first);
                indexList.Add(second);
                indexList.Add(first + 1);

                indexList.Add(second);
                indexList.Add(second + 1);
                indexList.Add(first + 1);
            }
        }

        indices = indexList.ToArray();
    }


}