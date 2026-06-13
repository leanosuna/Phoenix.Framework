using Phoenix.Framework.Rendering.Geometry.Vertices;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Phoenix.Framework.Rendering.Primitives;

public class Pyramid : Primitive
{
    public InfoPyramid PyramidInfo;

    public static Pyramid Create(InfoPyramid infoPyramid)
    {
        return PrimitiveHelper.Pyramid(infoPyramid);
    }
    public static Pyramid Create()
    {
        return PrimitiveHelper.Pyramid();
    }
    internal Pyramid(InfoPyramid pyramidInfo)
    {
        PyramidInfo = pyramidInfo;
        _primitiveInfo = pyramidInfo;
        BuildMesh();
    }

    protected override void VertexIndexBufferPos(ref VertexBufferBuilder vb, ref uint[] indices)
    {
        vb.Add(new Vector3(-0.5f, -0.5f, -0.5f));
        vb.Add(new Vector3(0.5f, -0.5f, -0.5f));
        vb.Add(new Vector3(0.5f, -0.5f, 0.5f));
        vb.Add(new Vector3(-0.5f, -0.5f, 0.5f));
        vb.Add(new Vector3(0f, 0.5f, 0f));

        indices =
        [
            0, 1, 1, 2, 2, 3, 3, 0,
            0, 4, 1, 4, 2, 4, 3, 4
        ];
    }

    protected override void VertexIndexBufferLines(ref VertexBufferBuilder vb, ref uint[] indices)
    {
        vb.Add(new Vector3(-0.5f, -0.5f, -0.5f));
        vb.Add(new Vector3(0.5f, -0.5f, -0.5f));
        vb.Add(new Vector3(0.5f, -0.5f, 0.5f));
        vb.Add(new Vector3(-0.5f, -0.5f, 0.5f));
        vb.Add(new Vector3(0f, 0.5f, 0f));

        indices =
        [
            0, 1, 1, 2, 2, 3, 3, 0,
            0, 4, 1, 4, 2, 4, 3, 4
        ];
    }

    protected override void VertexIndexBufferPosUv(ref VertexBufferBuilder vbb, ref uint[] indices)
    {
        var indexList = new List<uint>();

        // Base
        vbb.Add(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(0, 1));
        vbb.Add(new Vector3(0.5f, -0.5f, -0.5f), new Vector2(1, 1));
        vbb.Add(new Vector3(0.5f, -0.5f, 0.5f), new Vector2(1, 0));
        vbb.Add(new Vector3(-0.5f, -0.5f, 0.5f), new Vector2(0, 0));
        indexList.AddRange([0, 1, 2, 0, 2, 3]);

        // Side front (Z-)
        vbb.Add(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(0, 0));
        vbb.Add(new Vector3(0.5f, -0.5f, -0.5f), new Vector2(1, 0));
        vbb.Add(new Vector3(0f, 0.5f, 0f), new Vector2(0.5f, 1));
        indexList.AddRange([4, 6, 5]);

        // Side right (X+)
        vbb.Add(new Vector3(0.5f, -0.5f, -0.5f), new Vector2(0, 0));
        vbb.Add(new Vector3(0.5f, -0.5f, 0.5f), new Vector2(1, 0));
        vbb.Add(new Vector3(0f, 0.5f, 0f), new Vector2(0.5f, 1));
        indexList.AddRange([7, 9, 8]);

        // Side back (Z+)
        vbb.Add(new Vector3(0.5f, -0.5f, 0.5f), new Vector2(0, 0));
        vbb.Add(new Vector3(-0.5f, -0.5f, 0.5f), new Vector2(1, 0));
        vbb.Add(new Vector3(0f, 0.5f, 0f), new Vector2(0.5f, 1));
        indexList.AddRange([10, 12, 11]);

        // Side left (X-)
        vbb.Add(new Vector3(-0.5f, -0.5f, 0.5f), new Vector2(0, 0));
        vbb.Add(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(1, 0));
        vbb.Add(new Vector3(0f, 0.5f, 0f), new Vector2(0.5f, 1));
        indexList.AddRange([13, 15, 14]);

        indices = indexList.ToArray();
    }

    protected override void VertexIndexBufferPosUvNorm(ref VertexBufferBuilder vbb, ref uint[] indices)
    {
        var indexList = new List<uint>();

        // Base (Y-)
        var nBase = new Vector3(0, -1, 0);
        vbb.Add(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(0, 1), nBase);
        vbb.Add(new Vector3(0.5f, -0.5f, -0.5f), new Vector2(1, 1), nBase);
        vbb.Add(new Vector3(0.5f, -0.5f, 0.5f), new Vector2(1, 0), nBase);
        vbb.Add(new Vector3(-0.5f, -0.5f, 0.5f), new Vector2(0, 0), nBase);
        indexList.AddRange([0, 1, 2, 0, 2, 3]);

        // Side front (Z-)
        var nFront = Vector3.Normalize(new Vector3(0, 0.5f, -1));
        vbb.Add(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(0, 0), nFront);
        vbb.Add(new Vector3(0.5f, -0.5f, -0.5f), new Vector2(1, 0), nFront);
        vbb.Add(new Vector3(0f, 0.5f, 0f), new Vector2(0.5f, 1), nFront);
        indexList.AddRange([4, 6, 5]);

        // Side right (X+)
        var nRight = Vector3.Normalize(new Vector3(1, 0.5f, 0));
        vbb.Add(new Vector3(0.5f, -0.5f, -0.5f), new Vector2(0, 0), nRight);
        vbb.Add(new Vector3(0.5f, -0.5f, 0.5f), new Vector2(1, 0), nRight);
        vbb.Add(new Vector3(0f, 0.5f, 0f), new Vector2(0.5f, 1), nRight);
        indexList.AddRange([7, 9, 8]);

        // Side back (Z+)
        var nBack = Vector3.Normalize(new Vector3(0, 0.5f, 1));
        vbb.Add(new Vector3(0.5f, -0.5f, 0.5f), new Vector2(0, 0), nBack);
        vbb.Add(new Vector3(-0.5f, -0.5f, 0.5f), new Vector2(1, 0), nBack);
        vbb.Add(new Vector3(0f, 0.5f, 0f), new Vector2(0.5f, 1), nBack);
        indexList.AddRange([10, 12, 11]);

        // Side left (X-)
        var nLeft = Vector3.Normalize(new Vector3(-1, 0.5f, 0));
        vbb.Add(new Vector3(-0.5f, -0.5f, 0.5f), new Vector2(0, 0), nLeft);
        vbb.Add(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(1, 0), nLeft);
        vbb.Add(new Vector3(0f, 0.5f, 0f), new Vector2(0.5f, 1), nLeft);
        indexList.AddRange([13, 15, 14]);

        indices = indexList.ToArray();
    }

    protected override void VertexIndexBufferPosUvNormTaBt(ref VertexBufferBuilder vbb, ref uint[] indices)
    {
        var indexList = new List<uint>();

        // Base (Y-)
        var nBase = new Vector3(0, -1, 0);
        var tBase = new Vector3(1, 0, 0);
        var btBase = new Vector3(0, 0, 1);
        vbb.Add(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(0, 1), nBase, tBase, btBase);
        vbb.Add(new Vector3(0.5f, -0.5f, -0.5f), new Vector2(1, 1), nBase, tBase, btBase);
        vbb.Add(new Vector3(0.5f, -0.5f, 0.5f), new Vector2(1, 0), nBase, tBase, btBase);
        vbb.Add(new Vector3(-0.5f, -0.5f, 0.5f), new Vector2(0, 0), nBase, tBase, btBase);
        indexList.AddRange([0, 1, 2, 0, 2, 3]);

        // Side front (Z-)
        var nFront = Vector3.Normalize(new Vector3(0, 0.5f, -1));
        var tFront = Vector3.Normalize(new Vector3(1, 0, 0));
        var btFront = Vector3.Normalize(Vector3.Cross(nFront, tFront));
        vbb.Add(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(0, 0), nFront, tFront, btFront);
        vbb.Add(new Vector3(0.5f, -0.5f, -0.5f), new Vector2(1, 0), nFront, tFront, btFront);
        vbb.Add(new Vector3(0f, 0.5f, 0f), new Vector2(0.5f, 1), nFront, tFront, btFront);
        indexList.AddRange([4, 6, 5]);

        // Side right (X+)
        var nRight = Vector3.Normalize(new Vector3(1, 0.5f, 0));
        var tRight = Vector3.Normalize(new Vector3(0, 0, 1));
        var btRight = Vector3.Normalize(Vector3.Cross(nRight, tRight));
        vbb.Add(new Vector3(0.5f, -0.5f, -0.5f), new Vector2(0, 0), nRight, tRight, btRight);
        vbb.Add(new Vector3(0.5f, -0.5f, 0.5f), new Vector2(1, 0), nRight, tRight, btRight);
        vbb.Add(new Vector3(0f, 0.5f, 0f), new Vector2(0.5f, 1), nRight, tRight, btRight);
        indexList.AddRange([7, 9, 8]);

        // Side back (Z+)
        var nBack = Vector3.Normalize(new Vector3(0, 0.5f, 1));
        var tBack = Vector3.Normalize(new Vector3(-1, 0, 0));
        var btBack = Vector3.Normalize(Vector3.Cross(nBack, tBack));
        vbb.Add(new Vector3(0.5f, -0.5f, 0.5f), new Vector2(0, 0), nBack, tBack, btBack);
        vbb.Add(new Vector3(-0.5f, -0.5f, 0.5f), new Vector2(1, 0), nBack, tBack, btBack);
        vbb.Add(new Vector3(0f, 0.5f, 0f), new Vector2(0.5f, 1), nBack, tBack, btBack);
        indexList.AddRange([10, 12, 11]);

        // Side left (X-)
        var nLeft = Vector3.Normalize(new Vector3(-1, 0.5f, 0));
        var tLeft = Vector3.Normalize(new Vector3(0, 0, -1));
        var btLeft = Vector3.Normalize(Vector3.Cross(nLeft, tLeft));
        vbb.Add(new Vector3(-0.5f, -0.5f, 0.5f), new Vector2(0, 0), nLeft, tLeft, btLeft);
        vbb.Add(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(1, 0), nLeft, tLeft, btLeft);
        vbb.Add(new Vector3(0f, 0.5f, 0f), new Vector2(0.5f, 1), nLeft, tLeft, btLeft);
        indexList.AddRange([13, 15, 14]);

        indices = indexList.ToArray();
    }
}
