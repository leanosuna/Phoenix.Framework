using Phoenix.Framework.Rendering.Geometry.Model.Meshes;
using Phoenix.Framework.Rendering.Geometry.Vertices;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Phoenix.Framework.Rendering.Geometry.Model
{
    public class ModelMesh
    {
        public Matrix4x4 Transform { get; internal set; }
        public Mesh Mesh;
        public string Name;
        
        public ModelMesh(GL gl, string name, ModelVertex[] vertices, uint[] indices, Matrix4x4 transform, bool isAnimated, bool tangents)
        {
            Name = name;
            Transform = transform;

            var vertexDeclarationBuilder = new VertexDeclarationBuilder()
                .AddVertex3f() //Position
                .AddVertex2f() //TexCoord
                .AddVertex3f(); //Normals;

            if (tangents)
            {
                vertexDeclarationBuilder
                    .AddVertex3f() //Tangent
                    .AddVertex3f(); //BiTangent
            }
            if (isAnimated)
            {
                vertexDeclarationBuilder
                    .AddVertex4i() //BoneIds
                    .AddVertex4f(); //Weights
            }

            var vertexDeclaration = vertexDeclarationBuilder.Build();

            var vertexBufferBuilder = new VertexBufferBuilder(vertexDeclaration);

            PushData(vertexBufferBuilder, vertices, isAnimated, tangents);

            var verticesBytes = vertexBufferBuilder.Build();

            Mesh = new MeshBuilder<uint>(gl)
                .SetDrawType(PrimitiveType.Triangles)
                .SetVertexDeclaration(vertexDeclaration)
                .SetIndices(indices)
                .SetVertexData(verticesBytes)
                .Build();
        }

        private static void PushData(VertexBufferBuilder builder, ModelVertex[] vertices, bool isAnimated, bool tangents)
        {
            foreach(var v in vertices)
            {  
                var bid = new Vector4D<int>((int)v.BoneIds.X, (int)v.BoneIds.Y, (int)v.BoneIds.Z, (int)v.BoneIds.W);
                
                if (!tangents && !isAnimated)
                    builder.Add(v.Position, v.TexCoords, v.Normal);
                else if (tangents && !isAnimated)
                    builder.Add(v.Position, v.TexCoords, v.Normal, v.Tangent, v.Bitangent);
                else if (!tangents && isAnimated)
                    builder.Add(v.Position, v.TexCoords, v.Normal, bid, v.Weights);
                else if (tangents && isAnimated)
                    builder.Add(v.Position, v.TexCoords, v.Normal, v.Tangent, v.Bitangent, bid, v.Weights);
            }
        }
        public void Draw()
        {
            Mesh.Draw();
        }
    }
}
