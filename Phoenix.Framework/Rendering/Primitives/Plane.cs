using Phoenix.Framework.Rendering.Geometry.Vertices;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Phoenix.Framework.Rendering.Gizmos.Geometries.Primitives;

public class Plane : Primitive
{
    public InfoPlane PlaneInfo;

    public Plane(InfoPlane planeInfo)
    {
        PlaneInfo = planeInfo;
        _primitiveInfo = planeInfo;
        BuildMesh();
    }

    protected override void VertexIndexBufferPos(ref VertexBufferBuilder vb, ref uint[] indices)
    {
        vb.Add(new Vector3(-0.5f, 0f, -0.5f));
        vb.Add(new Vector3(0.5f, 0f, -0.5f));
        vb.Add(new Vector3(0.5f, 0f, 0.5f));
        vb.Add(new Vector3(-0.5f, 0f, 0.5f));

        indices =
        [
            0, 1,
            1, 2,
            2, 3,
            3, 0,
            0, 2
        ];
    }

    protected override void VertexIndexBufferLines(ref VertexBufferBuilder vb, ref uint[] indices)
    {
        vb.Add(new Vector3(-0.5f, 0f, -0.5f));
        vb.Add(new Vector3(0.5f, 0f, -0.5f));
        vb.Add(new Vector3(0.5f, 0f, 0.5f));
        vb.Add(new Vector3(-0.5f, 0f, 0.5f));

        indices =
        [
            0, 1,
            1, 2,
            2, 3,
            3, 0,
            0, 2
        ];
    }

    protected override void VertexIndexBufferPosUv(ref VertexBufferBuilder vbb, ref uint[] indices)
    {
        vbb.Add(new Vector3(-0.5f, 0f, -0.5f), new Vector2(0, 1));
        vbb.Add(new Vector3(0.5f, 0f, -0.5f), new Vector2(1, 1));
        vbb.Add(new Vector3(0.5f, 0f, 0.5f), new Vector2(1, 0));
        vbb.Add(new Vector3(-0.5f, 0f, 0.5f), new Vector2(0, 0));

        indices = [0, 1, 2, 0, 2, 3];
    }

    protected override void VertexIndexBufferPosUvNorm(ref VertexBufferBuilder vbb, ref uint[] indices)
    {
        var normal = new Vector3(0, 1, 0);
        vbb.Add(new Vector3(-0.5f, 0f, -0.5f), new Vector2(0, 1), normal);
        vbb.Add(new Vector3(0.5f, 0f, -0.5f), new Vector2(1, 1), normal);
        vbb.Add(new Vector3(0.5f, 0f, 0.5f), new Vector2(1, 0), normal);
        vbb.Add(new Vector3(-0.5f, 0f, 0.5f), new Vector2(0, 0), normal);

        indices = [0, 1, 2, 0, 2, 3];
    }

    protected override void VertexIndexBufferPosUvNormTaBt(ref VertexBufferBuilder vbb, ref uint[] indices)
    {
        var normal = new Vector3(0, 1, 0);
        var tangent = new Vector3(1, 0, 0);
        var bitangent = new Vector3(0, 0, -1);
        vbb.Add(new Vector3(-0.5f, 0f, -0.5f), new Vector2(0, 1), normal, tangent, bitangent);
        vbb.Add(new Vector3(0.5f, 0f, -0.5f), new Vector2(1, 1), normal, tangent, bitangent);
        vbb.Add(new Vector3(0.5f, 0f, 0.5f), new Vector2(1, 0), normal, tangent, bitangent);
        vbb.Add(new Vector3(-0.5f, 0f, 0.5f), new Vector2(0, 0), normal, tangent, bitangent);

        indices = [0, 1, 2, 0, 2, 3];
    }
}
