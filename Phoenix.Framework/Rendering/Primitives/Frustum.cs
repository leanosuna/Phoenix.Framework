using Phoenix.Framework.Rendering.Geometry.Vertices;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Phoenix.Framework.Rendering.Gizmos.Geometries.Primitives;

public class Frustum : Primitive
{
    public InfoFrustum FrustumInfo;

    public Frustum(InfoFrustum frustumInfo)
    {
        FrustumInfo = frustumInfo;
        _primitiveInfo = frustumInfo;
        BuildMesh();
    }

    private (Vector3, Vector3, Vector3, Vector3, Vector3, Vector3, Vector3, Vector3) GetCorners()
    {
        var nw = FrustumInfo.NearWidth * 0.5f;
        var nh = FrustumInfo.NearHeight * 0.5f;
        var fw = FrustumInfo.FarWidth * 0.5f;
        var fh = FrustumInfo.FarHeight * 0.5f;
        var d = FrustumInfo.Depth * 0.5f;

        var ntl = new Vector3(-nw, +nh, +d);
        var ntr = new Vector3(+nw, +nh, +d);
        var nbr = new Vector3(+nw, -nh, +d);
        var nbl = new Vector3(-nw, -nh, +d);
        var ftl = new Vector3(-fw, +fh, -d);
        var ftr = new Vector3(+fw, +fh, -d);
        var fbr = new Vector3(+fw, -fh, -d);
        var fbl = new Vector3(-fw, -fh, -d);

        return (ntl, ntr, nbr, nbl, ftl, ftr, fbr, fbl);
    }

    protected override void VertexIndexBufferPos(ref VertexBufferBuilder vb, ref uint[] indices)
    {
        var (ntl, ntr, nbr, nbl, ftl, ftr, fbr, fbl) = GetCorners();

        vb.Add(ntl); vb.Add(ntr); vb.Add(nbr); vb.Add(nbl);
        vb.Add(ftl); vb.Add(ftr); vb.Add(fbr); vb.Add(fbl);

        indices =
        [
            0, 1, 1, 2, 2, 3, 3, 0,
            4, 5, 5, 6, 6, 7, 7, 4,
            0, 4, 1, 5, 2, 6, 3, 7
        ];
    }

    protected override void VertexIndexBufferLines(ref VertexBufferBuilder vb, ref uint[] indices)
    {
        var (ntl, ntr, nbr, nbl, ftl, ftr, fbr, fbl) = GetCorners();

        vb.Add(ntl); vb.Add(ntr); vb.Add(nbr); vb.Add(nbl);
        vb.Add(ftl); vb.Add(ftr); vb.Add(fbr); vb.Add(fbl);

        indices =
        [
            0, 1, 1, 2, 2, 3, 3, 0,
            4, 5, 5, 6, 6, 7, 7, 4,
            0, 4, 1, 5, 2, 6, 3, 7
        ];
    }

    protected override void VertexIndexBufferPosUv(ref VertexBufferBuilder vbb, ref uint[] indices)
    {
        var (ntl, ntr, nbr, nbl, ftl, ftr, fbr, fbl) = GetCorners();

        // Near
        vbb.Add(ntl, new Vector2(0, 1));
        vbb.Add(ntr, new Vector2(1, 1));
        vbb.Add(nbr, new Vector2(1, 0));
        vbb.Add(nbl, new Vector2(0, 0));

        // Far
        vbb.Add(ftl, new Vector2(0, 1));
        vbb.Add(ftr, new Vector2(1, 1));
        vbb.Add(fbr, new Vector2(1, 0));
        vbb.Add(fbl, new Vector2(0, 0));

        // Top
        vbb.Add(ntl, new Vector2(0, 1));
        vbb.Add(ftl, new Vector2(1, 1));
        vbb.Add(ftr, new Vector2(1, 0));
        vbb.Add(ntr, new Vector2(0, 0));

        // Bottom
        vbb.Add(nbl, new Vector2(0, 1));
        vbb.Add(nbr, new Vector2(1, 1));
        vbb.Add(fbr, new Vector2(1, 0));
        vbb.Add(fbl, new Vector2(0, 0));

        // Left
        vbb.Add(ntl, new Vector2(0, 1));
        vbb.Add(nbl, new Vector2(1, 1));
        vbb.Add(fbl, new Vector2(1, 0));
        vbb.Add(ftl, new Vector2(0, 0));

        // Right
        vbb.Add(ntr, new Vector2(0, 1));
        vbb.Add(ftr, new Vector2(1, 1));
        vbb.Add(fbr, new Vector2(1, 0));
        vbb.Add(nbr, new Vector2(0, 0));

        indices =
        [
            0, 1, 2, 0, 2, 3,
            4, 5, 6, 4, 6, 7,
            8, 9, 10, 8, 10, 11,
            12, 13, 14, 12, 14, 15,
            16, 17, 18, 16, 18, 19,
            20, 21, 22, 20, 22, 23
        ];
    }

    protected override void VertexIndexBufferPosUvNorm(ref VertexBufferBuilder vbb, ref uint[] indices)
    {
        var (ntl, ntr, nbr, nbl, ftl, ftr, fbr, fbl) = GetCorners();

        // Near (Z+)
        var nNear = Vector3.Normalize(Vector3.Cross(ntr - ntl, nbr - ntr));
        vbb.Add(ntl, new Vector2(0, 1), nNear);
        vbb.Add(ntr, new Vector2(1, 1), nNear);
        vbb.Add(nbr, new Vector2(1, 0), nNear);
        vbb.Add(nbl, new Vector2(0, 0), nNear);

        // Far (Z-)
        var nFar = Vector3.Normalize(Vector3.Cross(fbl - ftl, ftr - ftl));
        vbb.Add(ftl, new Vector2(0, 1), nFar);
        vbb.Add(ftr, new Vector2(1, 1), nFar);
        vbb.Add(fbr, new Vector2(1, 0), nFar);
        vbb.Add(fbl, new Vector2(0, 0), nFar);

        // Top
        var nTop = Vector3.Normalize(Vector3.Cross(ftl - ntl, ftr - ftl));
        vbb.Add(ntl, new Vector2(0, 1), nTop);
        vbb.Add(ftl, new Vector2(1, 1), nTop);
        vbb.Add(ftr, new Vector2(1, 0), nTop);
        vbb.Add(ntr, new Vector2(0, 0), nTop);

        // Bottom
        var nBottom = Vector3.Normalize(Vector3.Cross(nbr - nbl, fbr - nbr));
        vbb.Add(nbl, new Vector2(0, 1), nBottom);
        vbb.Add(nbr, new Vector2(1, 1), nBottom);
        vbb.Add(fbr, new Vector2(1, 0), nBottom);
        vbb.Add(fbl, new Vector2(0, 0), nBottom);

        // Left
        var nLeft = Vector3.Normalize(Vector3.Cross(nbl - ntl, fbl - nbl));
        vbb.Add(ntl, new Vector2(0, 1), nLeft);
        vbb.Add(nbl, new Vector2(1, 1), nLeft);
        vbb.Add(fbl, new Vector2(1, 0), nLeft);
        vbb.Add(ftl, new Vector2(0, 0), nLeft);

        // Right
        var nRight = Vector3.Normalize(Vector3.Cross(ftr - ntr, fbr - ftr));
        vbb.Add(ntr, new Vector2(0, 1), nRight);
        vbb.Add(ftr, new Vector2(1, 1), nRight);
        vbb.Add(fbr, new Vector2(1, 0), nRight);
        vbb.Add(nbr, new Vector2(0, 0), nRight);

        indices =
        [
            0, 1, 2, 0, 2, 3,
            4, 5, 6, 4, 6, 7,
            8, 9, 10, 8, 10, 11,
            12, 13, 14, 12, 14, 15,
            16, 17, 18, 16, 18, 19,
            20, 21, 22, 20, 22, 23
        ];
    }

    protected override void VertexIndexBufferPosUvNormTaBt(ref VertexBufferBuilder vbb, ref uint[] indices)
    {
        var (ntl, ntr, nbr, nbl, ftl, ftr, fbr, fbl) = GetCorners();

        // Near (Z+)
        var nNear = Vector3.Normalize(Vector3.Cross(ntr - ntl, nbr - ntr));
        var tNear = Vector3.Normalize(ntr - ntl);
        var btNear = Vector3.Normalize(Vector3.Cross(nNear, tNear));
        vbb.Add(ntl, new Vector2(0, 1), nNear, tNear, btNear);
        vbb.Add(ntr, new Vector2(1, 1), nNear, tNear, btNear);
        vbb.Add(nbr, new Vector2(1, 0), nNear, tNear, btNear);
        vbb.Add(nbl, new Vector2(0, 0), nNear, tNear, btNear);

        // Far (Z-)
        var nFar = Vector3.Normalize(Vector3.Cross(fbl - ftl, ftr - ftl));
        var tFar = Vector3.Normalize(ftr - ftl);
        var btFar = Vector3.Normalize(Vector3.Cross(nFar, tFar));
        vbb.Add(ftl, new Vector2(0, 1), nFar, tFar, btFar);
        vbb.Add(ftr, new Vector2(1, 1), nFar, tFar, btFar);
        vbb.Add(fbr, new Vector2(1, 0), nFar, tFar, btFar);
        vbb.Add(fbl, new Vector2(0, 0), nFar, tFar, btFar);

        // Top
        var nTop = Vector3.Normalize(Vector3.Cross(ftl - ntl, ftr - ftl));
        var tTop = Vector3.Normalize(ftr - ftl);
        var btTop = Vector3.Normalize(Vector3.Cross(nTop, tTop));
        vbb.Add(ntl, new Vector2(0, 1), nTop, tTop, btTop);
        vbb.Add(ftl, new Vector2(1, 1), nTop, tTop, btTop);
        vbb.Add(ftr, new Vector2(1, 0), nTop, tTop, btTop);
        vbb.Add(ntr, new Vector2(0, 0), nTop, tTop, btTop);

        // Bottom
        var nBottom = Vector3.Normalize(Vector3.Cross(nbr - nbl, fbr - nbr));
        var tBottom = Vector3.Normalize(nbr - nbl);
        var btBottom = Vector3.Normalize(Vector3.Cross(nBottom, tBottom));
        vbb.Add(nbl, new Vector2(0, 1), nBottom, tBottom, btBottom);
        vbb.Add(nbr, new Vector2(1, 1), nBottom, tBottom, btBottom);
        vbb.Add(fbr, new Vector2(1, 0), nBottom, tBottom, btBottom);
        vbb.Add(fbl, new Vector2(0, 0), nBottom, tBottom, btBottom);

        // Left
        var nLeft = Vector3.Normalize(Vector3.Cross(nbl - ntl, fbl - nbl));
        var tLeft = Vector3.Normalize(nbl - ntl);
        var btLeft = Vector3.Normalize(Vector3.Cross(nLeft, tLeft));
        vbb.Add(ntl, new Vector2(0, 1), nLeft, tLeft, btLeft);
        vbb.Add(nbl, new Vector2(1, 1), nLeft, tLeft, btLeft);
        vbb.Add(fbl, new Vector2(1, 0), nLeft, tLeft, btLeft);
        vbb.Add(ftl, new Vector2(0, 0), nLeft, tLeft, btLeft);

        // Right
        var nRight = Vector3.Normalize(Vector3.Cross(ftr - ntr, fbr - ftr));
        var tRight = Vector3.Normalize(ftr - ntr);
        var btRight = Vector3.Normalize(Vector3.Cross(nRight, tRight));
        vbb.Add(ntr, new Vector2(0, 1), nRight, tRight, btRight);
        vbb.Add(ftr, new Vector2(1, 1), nRight, tRight, btRight);
        vbb.Add(fbr, new Vector2(1, 0), nRight, tRight, btRight);
        vbb.Add(nbr, new Vector2(0, 0), nRight, tRight, btRight);

        indices =
        [
            0, 1, 2, 0, 2, 3,
            4, 5, 6, 4, 6, 7,
            8, 9, 10, 8, 10, 11,
            12, 13, 14, 12, 14, 15,
            16, 17, 18, 16, 18, 19,
            20, 21, 22, 20, 22, 23
        ];
    }
}
