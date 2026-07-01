using Phoenix.Framework.Maths;
using Phoenix.Framework.Rendering.Geometry.Model;
using Phoenix.Framework.Rendering.Geometry.Model.Meshes;
using Phoenix.Framework.Rendering.Geometry.Vertices;
using Silk.NET.GLFW;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Phoenix.Framework.Rendering.Primitives;

public abstract class Primitive
{
    private Mesh<uint> _mesh;
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

    public T[] GetVertexData<T>() where T : unmanaged => _mesh.GetVertexData<T>();
    public uint[] GetIndexData() => _mesh.GetIndexData();
    protected void BuildMesh()
    {
        var vdd = new VertexDeclarationBuilder().AddVertex3f();

        var uv = _primitiveInfo.Uv;
        var n = _primitiveInfo.Normals;
        var t = _primitiveInfo.Tangents;
        var bt = _primitiveInfo.Bitangents;

        if (_primitiveInfo.MeshPrimitiveType == PrimitiveType.Lines)
        {
            var lineVd = vdd.Build();
            var lineVbb = VertexBufferBuilder.BuildPos();
            uint[] lineIndices = [];
            VertexIndexBufferLines(ref lineVbb, ref lineIndices);
            var lineVertices = lineVbb.Build();
            _mesh = new Mesh<uint>(GL, PrimitiveType.Lines, lineVd, lineIndices, lineVertices);
            return;
        }

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

        var vertices = vbb.Build();
        _mesh = new Mesh<uint>(GL, _primitiveInfo.MeshPrimitiveType, vd, indices, vertices, _primitiveInfo.SaveVertices);
    }

    protected abstract void VertexIndexBufferPos(ref VertexBufferBuilder vbb, ref uint[] indices);
    protected abstract void VertexIndexBufferPosUv(ref VertexBufferBuilder vbb, ref uint[] indices);
    protected abstract void VertexIndexBufferPosUvNorm(ref VertexBufferBuilder vbb, ref uint[] indices);
    protected abstract void VertexIndexBufferPosUvNormTaBt(ref VertexBufferBuilder vbb, ref uint[] indices);
    protected abstract void VertexIndexBufferLines(ref VertexBufferBuilder vbb, ref uint[] indices);
}