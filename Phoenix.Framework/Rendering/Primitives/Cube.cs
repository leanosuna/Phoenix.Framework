using System.Numerics;
using Phoenix.Framework.Rendering.Geometry.Model.Meshes;
using Phoenix.Framework.Rendering.Geometry.Vertices;
using Silk.NET.OpenGL;

namespace Phoenix.Framework.Rendering.Gizmos.Geometries.Primitives;

public class Cube : Primitive
{
    public InfoCube CubeInfo;

    public Cube(InfoCube cubeInfo) 
    {
        CubeInfo = cubeInfo;
        _primitiveInfo = cubeInfo;
        BuildMesh();
        
    }
    protected override void VertexIndexBufferPos(ref VertexBufferBuilder vb, ref uint[] indices)
    {
        vb.Add(new Vector3(0.5f, 0.5f, 0.5f));
        vb.Add(new Vector3(-0.5f, 0.5f, 0.5f));
        vb.Add(new Vector3(0.5f, -0.5f, 0.5f));
        vb.Add(new Vector3(-0.5f, -0.5f, 0.5f));
        vb.Add(new Vector3(0.5f, 0.5f, -0.5f));
        vb.Add(new Vector3(-0.5f, 0.5f, -0.5f));
        vb.Add(new Vector3(0.5f, -0.5f, -0.5f));
        vb.Add(new Vector3(-0.5f, -0.5f, -0.5f));

        indices =
        [
            0, 1,
            0, 2,
            1, 3,
            3, 2,

            4, 5,
            4, 6,
            5, 7,
            7, 6,

            0, 4,
            1, 5,
            2, 6,
            3, 7
        ];
    }

    protected override void VertexIndexBufferLines(ref VertexBufferBuilder vb, ref uint[] indices)
    {
        vb.Add(new Vector3(0.5f, 0.5f, 0.5f));
        vb.Add(new Vector3(-0.5f, 0.5f, 0.5f));
        vb.Add(new Vector3(0.5f, -0.5f, 0.5f));
        vb.Add(new Vector3(-0.5f, -0.5f, 0.5f));
        vb.Add(new Vector3(0.5f, 0.5f, -0.5f));
        vb.Add(new Vector3(-0.5f, 0.5f, -0.5f));
        vb.Add(new Vector3(0.5f, -0.5f, -0.5f));
        vb.Add(new Vector3(-0.5f, -0.5f, -0.5f));

        indices =
        [
            0, 1,
            0, 2,
            1, 3,
            3, 2,

            4, 5,
            4, 6,
            5, 7,
            7, 6,

            0, 4,
            1, 5,
            2, 6,
            3, 7
        ];
    }

    protected override void VertexIndexBufferPosUv(ref VertexBufferBuilder vbb, ref uint[] indices)
    {
        vbb.Add(new Vector3(-0.5f, -0.5f, 0.5f), new Vector2(0, 1));
        vbb.Add(new Vector3(0.5f, -0.5f, 0.5f), new Vector2(1, 1));
        vbb.Add(new Vector3(0.5f, 0.5f, 0.5f), new Vector2(1, 0));
        vbb.Add(new Vector3(-0.5f, 0.5f, 0.5f), new Vector2(0, 0));
        vbb.Add(new Vector3(0.5f, -0.5f, -0.5f), new Vector2(0, 1));
        vbb.Add(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(1, 1));
        vbb.Add(new Vector3(-0.5f, 0.5f, -0.5f), new Vector2(1, 0));
        vbb.Add(new Vector3(0.5f, 0.5f, -0.5f), new Vector2(0, 0));
        vbb.Add(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(0, 1));
        vbb.Add(new Vector3(-0.5f, -0.5f, 0.5f), new Vector2(1, 1));
        vbb.Add(new Vector3(-0.5f, 0.5f, 0.5f), new Vector2(1, 0));
        vbb.Add(new Vector3(-0.5f, 0.5f, -0.5f), new Vector2(0, 0));
        vbb.Add(new Vector3(0.5f, -0.5f, 0.5f), new Vector2(0, 1));
        vbb.Add(new Vector3(0.5f, -0.5f, -0.5f), new Vector2(1, 1));
        vbb.Add(new Vector3(0.5f, 0.5f, -0.5f), new Vector2(1, 0));
        vbb.Add(new Vector3(0.5f, 0.5f, 0.5f), new Vector2(0, 0));
        vbb.Add(new Vector3(-0.5f, 0.5f, 0.5f), new Vector2(0, 1));
        vbb.Add(new Vector3(0.5f, 0.5f, 0.5f), new Vector2(1, 1));
        vbb.Add(new Vector3(0.5f, 0.5f, -0.5f), new Vector2(1, 0));
        vbb.Add(new Vector3(-0.5f, 0.5f, -0.5f), new Vector2(0, 0));
        vbb.Add(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(0, 1));
        vbb.Add(new Vector3(0.5f, -0.5f, -0.5f), new Vector2(1, 1));
        vbb.Add(new Vector3(0.5f, -0.5f, 0.5f), new Vector2(1, 0));
        vbb.Add(new Vector3(-0.5f, -0.5f, 0.5f), new Vector2(0, 0));

        indices = [
            // Front
            0, 1, 2,
            0, 2, 3,

            // Back
            4, 5, 6,
            4, 6, 7,

            // Left
            8, 9,10,
            8,10,11,

            // Right
            12,13,14,
            12,14,15,

            // Top
            16,17,18,
            16,18,19,

            // Bottom
            20,21,22,
            20,22,23
        ];
    }

    protected override void VertexIndexBufferPosUvNorm(ref VertexBufferBuilder vbb, ref uint[] indices)
    {
        // Front (Z+)
        vbb.Add(new Vector3(-0.5f, -0.5f, 0.5f), new Vector2(0, 1), new Vector3(0, 0, 1));
        vbb.Add(new Vector3(0.5f, -0.5f, 0.5f), new Vector2(1, 1), new Vector3(0, 0, 1));
        vbb.Add(new Vector3(0.5f, 0.5f, 0.5f), new Vector2(1, 0), new Vector3(0, 0, 1));
        vbb.Add(new Vector3(-0.5f, 0.5f, 0.5f), new Vector2(0, 0), new Vector3(0, 0, 1));
        // Back (Z-)
        vbb.Add(new Vector3(0.5f, -0.5f, -0.5f), new Vector2(0, 1), new Vector3(0, 0, -1));
        vbb.Add(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(1, 1), new Vector3(0, 0, -1));
        vbb.Add(new Vector3(-0.5f, 0.5f, -0.5f), new Vector2(1, 0), new Vector3(0, 0, -1));
        vbb.Add(new Vector3(0.5f, 0.5f, -0.5f), new Vector2(0, 0), new Vector3(0, 0, -1));
        // Left (X-)
        vbb.Add(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(0, 1), new Vector3(-1, 0, 0));
        vbb.Add(new Vector3(-0.5f, -0.5f, 0.5f), new Vector2(1, 1), new Vector3(-1, 0, 0));
        vbb.Add(new Vector3(-0.5f, 0.5f, 0.5f), new Vector2(1, 0), new Vector3(-1, 0, 0));
        vbb.Add(new Vector3(-0.5f, 0.5f, -0.5f), new Vector2(0, 0), new Vector3(-1, 0, 0));
        // Right (X+)
        vbb.Add(new Vector3(0.5f, -0.5f, 0.5f), new Vector2(0, 1), new Vector3(1, 0, 0));
        vbb.Add(new Vector3(0.5f, -0.5f, -0.5f), new Vector2(1, 1), new Vector3(1, 0, 0));
        vbb.Add(new Vector3(0.5f, 0.5f, -0.5f), new Vector2(1, 0), new Vector3(1, 0, 0));
        vbb.Add(new Vector3(0.5f, 0.5f, 0.5f), new Vector2(0, 0), new Vector3(1, 0, 0));
        // Top (Y+)
        vbb.Add(new Vector3(-0.5f, 0.5f, 0.5f), new Vector2(0, 1), new Vector3(0, 1, 0));
        vbb.Add(new Vector3(0.5f, 0.5f, 0.5f), new Vector2(1, 1), new Vector3(0, 1, 0));
        vbb.Add(new Vector3(0.5f, 0.5f, -0.5f), new Vector2(1, 0), new Vector3(0, 1, 0));
        vbb.Add(new Vector3(-0.5f, 0.5f, -0.5f), new Vector2(0, 0), new Vector3(0, 1, 0));
        // Bottom (Y-)
        vbb.Add(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(0, 1), new Vector3(0, -1, 0));
        vbb.Add(new Vector3(0.5f, -0.5f, -0.5f), new Vector2(1, 1), new Vector3(0, -1, 0));
        vbb.Add(new Vector3(0.5f, -0.5f, 0.5f), new Vector2(1, 0), new Vector3(0, -1, 0));
        vbb.Add(new Vector3(-0.5f, -0.5f, 0.5f), new Vector2(0, 0), new Vector3(0, -1, 0));

        indices = [
            0, 1, 2, 0, 2, 3,
            4, 5, 6, 4, 6, 7,
            8, 9,10, 8,10,11,
            12,13,14, 12,14,15,
            16,17,18, 16,18,19,
            20,21,22, 20,22,23
        ];
    }

    protected override void VertexIndexBufferPosUvNormTaBt(ref VertexBufferBuilder vbb, ref uint[] indices)
    {
        // Front (Z+)
        vbb.Add(new Vector3(-0.5f, -0.5f, 0.5f), new Vector2(0, 1), new Vector3(0, 0, 1), new Vector3(1, 0, 0), new Vector3(0, 1, 0));
        vbb.Add(new Vector3(0.5f, -0.5f, 0.5f), new Vector2(1, 1), new Vector3(0, 0, 1), new Vector3(1, 0, 0), new Vector3(0, 1, 0));
        vbb.Add(new Vector3(0.5f, 0.5f, 0.5f), new Vector2(1, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 0), new Vector3(0, 1, 0));
        vbb.Add(new Vector3(-0.5f, 0.5f, 0.5f), new Vector2(0, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 0), new Vector3(0, 1, 0));
        // Back (Z-)
        vbb.Add(new Vector3(0.5f, -0.5f, -0.5f), new Vector2(0, 1), new Vector3(0, 0, -1), new Vector3(-1, 0, 0), new Vector3(0, 1, 0));
        vbb.Add(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(1, 1), new Vector3(0, 0, -1), new Vector3(-1, 0, 0), new Vector3(0, 1, 0));
        vbb.Add(new Vector3(-0.5f, 0.5f, -0.5f), new Vector2(1, 0), new Vector3(0, 0, -1), new Vector3(-1, 0, 0), new Vector3(0, 1, 0));
        vbb.Add(new Vector3(0.5f, 0.5f, -0.5f), new Vector2(0, 0), new Vector3(0, 0, -1), new Vector3(-1, 0, 0), new Vector3(0, 1, 0));
        // Left (X-)
        vbb.Add(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(0, 1), new Vector3(-1, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 0));
        vbb.Add(new Vector3(-0.5f, -0.5f, 0.5f), new Vector2(1, 1), new Vector3(-1, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 0));
        vbb.Add(new Vector3(-0.5f, 0.5f, 0.5f), new Vector2(1, 0), new Vector3(-1, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 0));
        vbb.Add(new Vector3(-0.5f, 0.5f, -0.5f), new Vector2(0, 0), new Vector3(-1, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 0));
        // Right (X+)
        vbb.Add(new Vector3(0.5f, -0.5f, 0.5f), new Vector2(0, 1), new Vector3(1, 0, 0), new Vector3(0, 0, -1), new Vector3(0, 1, 0));
        vbb.Add(new Vector3(0.5f, -0.5f, -0.5f), new Vector2(1, 1), new Vector3(1, 0, 0), new Vector3(0, 0, -1), new Vector3(0, 1, 0));
        vbb.Add(new Vector3(0.5f, 0.5f, -0.5f), new Vector2(1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, -1), new Vector3(0, 1, 0));
        vbb.Add(new Vector3(0.5f, 0.5f, 0.5f), new Vector2(0, 0), new Vector3(1, 0, 0), new Vector3(0, 0, -1), new Vector3(0, 1, 0));
        // Top (Y+)
        vbb.Add(new Vector3(-0.5f, 0.5f, 0.5f), new Vector2(0, 1), new Vector3(0, 1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, -1));
        vbb.Add(new Vector3(0.5f, 0.5f, 0.5f), new Vector2(1, 1), new Vector3(0, 1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, -1));
        vbb.Add(new Vector3(0.5f, 0.5f, -0.5f), new Vector2(1, 0), new Vector3(0, 1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, -1));
        vbb.Add(new Vector3(-0.5f, 0.5f, -0.5f), new Vector2(0, 0), new Vector3(0, 1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, -1));
        // Bottom (Y-)
        vbb.Add(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(0, 1), new Vector3(0, -1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1));
        vbb.Add(new Vector3(0.5f, -0.5f, -0.5f), new Vector2(1, 1), new Vector3(0, -1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1));
        vbb.Add(new Vector3(0.5f, -0.5f, 0.5f), new Vector2(1, 0), new Vector3(0, -1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1));
        vbb.Add(new Vector3(-0.5f, -0.5f, 0.5f), new Vector2(0, 0), new Vector3(0, -1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1));

        indices = [
            0, 1, 2, 0, 2, 3,
            4, 5, 6, 4, 6, 7,
            8, 9,10, 8,10,11,
            12,13,14, 12,14,15,
            16,17,18, 16,18,19,
            20,21,22, 20,22,23
        ];
    }
}