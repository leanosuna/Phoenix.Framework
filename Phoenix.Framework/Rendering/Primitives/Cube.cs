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

        BuildMesh();
    }

    public override void BuildMesh()
    {
        var vdd = new VertexDeclarationBuilder().AddVertex3f();

        var uv = CubeInfo.Uv;
        var n = CubeInfo.Normals;
        var t = CubeInfo.Tangents;
        var bt = CubeInfo.Bitangents;
        
        if(uv)
            vdd.AddVertex2f();
        if(n)
            vdd.AddVertex3f();
        if(t)
            vdd.AddVertex3f();
        if(bt)
            vdd.AddVertex3f();
            
        var vd = vdd.Build();

        VertexBufferBuilder vbb = VertexBufferBuilder.BuildPos();
        uint[] indices = []; 
        if(!uv && !n)
        {
            VertexIndexBufferPos(ref vbb, ref indices);
        }
        else if(uv && !n)
        {
            vbb = VertexBufferBuilder.BuildPosUv();
            VertexIndexBufferPosUv(ref vbb, ref indices);
        }
        else if(uv && n)
        {
            vbb = VertexBufferBuilder.BuildPosUvNorm();
            // VertexIndexBufferPosUvNorm(ref vbb, ref indices);
        }
        
        vbb.Build();

        var vertices = vbb.Build();
        _mesh = new MeshBuilder<uint>(GL)
            .SetDrawType(CubeInfo.MeshPrimitiveType)
            .SetVertexDeclaration(vd)
            .SetIndices(indices)
            .SetVertexData(vertices)
            .Build();
    }

    private void VertexIndexBufferPos(ref VertexBufferBuilder vb, ref uint[] indices)
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

    private void VertexIndexBufferPosUv(ref VertexBufferBuilder vbb, ref uint[] indices)
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


}