using Phoenix.Framework.Maths;
using Phoenix.Framework.Rendering.Geometry.Model;
using Phoenix.Framework.Rendering.Geometry.Model.Meshes;
using Phoenix.Framework.Rendering.Geometry.Vertices;
using Silk.NET.GLFW;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Phoenix.Framework.Rendering.Gizmos.Geometries.Primitives;

public abstract class Primitive
{
    protected Mesh _mesh;
    protected static GL GL;
    protected PrimitiveInfo _primitiveInfo;
    public static void SetGL(GL gl)
    {
        GL = gl;
    }
    public void Draw()
    {
        _mesh.Draw();
    }    

    public void BuildMesh()
    {
        var vdd = new VertexDeclarationBuilder().AddVertex3f();

        var uv = _primitiveInfo.Uv;
        var n = _primitiveInfo.Normals;
        var t = _primitiveInfo.Tangents;
        var bt = _primitiveInfo.Bitangents;

        if (uv)
            vdd.AddVertex2f();
        if (n)
            vdd.AddVertex3f();
        if (t)
            vdd.AddVertex3f();
        if (bt)
            vdd.AddVertex3f();

        var vd = vdd.Build();

        VertexBufferBuilder vbb = VertexBufferBuilder.BuildPos();
        uint[] indices = [];
        if (!uv && !n)
        {
            VertexIndexBufferPos(ref vbb, ref indices);
        }
        else if (uv && !n)
        {
            vbb = VertexBufferBuilder.BuildPosUv();
            VertexIndexBufferPosUv(ref vbb, ref indices);
        }
        else if (uv && n && !t && !bt)
        {
            vbb = VertexBufferBuilder.BuildPosUvNorm();
            VertexIndexBufferPosUvNorm(ref vbb, ref indices);
        }
        else if (uv && n && t && bt)
        {
            vbb = VertexBufferBuilder.BuildPosUvNormTanBt();
            VertexIndexBufferPosUvNormTaBt(ref vbb, ref indices);
        }

        if (_primitiveInfo.MeshPrimitiveType == PrimitiveType.Lines && uv)
            indices = TrianglesToLines(indices);

        var vertices = vbb.Build();
        _mesh = new MeshBuilder<uint>(GL)
            .SetDrawType(_primitiveInfo.MeshPrimitiveType)
            .SetVertexDeclaration(vd)
            .SetIndices(indices)
            .SetVertexData(vertices)
            .Build();
    }

    protected abstract void VertexIndexBufferPos(ref VertexBufferBuilder vbb, ref uint[] indices);
    protected abstract void VertexIndexBufferPosUv(ref VertexBufferBuilder vbb, ref uint[] indices);
    protected abstract void VertexIndexBufferPosUvNorm(ref VertexBufferBuilder vbb, ref uint[] indices);
    protected abstract void VertexIndexBufferPosUvNormTaBt(ref VertexBufferBuilder vbb, ref uint[] indices);

    protected static uint[] TrianglesToLines(uint[] triangleIndices)
    {
        var lines = new List<uint>(triangleIndices.Length * 2);
        for (var i = 0; i < triangleIndices.Length; i += 3)
        {
            lines.Add(triangleIndices[i]);
            lines.Add(triangleIndices[i + 1]);
            lines.Add(triangleIndices[i + 1]);
            lines.Add(triangleIndices[i + 2]);
            lines.Add(triangleIndices[i + 2]);
            lines.Add(triangleIndices[i]);
        }
        return lines.ToArray();
    }
}