using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Phoenix.Framework.Rendering.Geometry.Vertices
{
    public class VertexBufferBuilder
    {
        public VertexDeclaration VertexDeclaration;

        private MemoryStream _memory = new();
        private VertexBuilder _vb;
        public VertexBufferBuilder(VertexDeclaration vertexDeclaration)
        {
            VertexDeclaration = vertexDeclaration;
            _vb = new(vertexDeclaration);
        }

        public VertexBufferBuilder AddVertex(ReadOnlySpan<byte> data)
        {
            if (data.Length != VertexDeclaration.StrideBytes)
                throw new Exception($"size missmatch: stride {VertexDeclaration.StrideBytes} - vertex {data.Length}");

            _memory.Write(data);
            return this;
        }

        public VertexBufferBuilder AddVertex(Action<VertexBuilder> build)
        {
            _vb.Reset();
            build(_vb);
            _memory.Write(_vb.Build());
            
            return this;
        }

        public static VertexBufferBuilder BuildPos()
        {
            var vd = new VertexDeclarationBuilder()
                .AddVertex3f()
                .Build();
            return new VertexBufferBuilder(vd);
        }

        public VertexBufferBuilder Add(Vector3 pos)
        {
            AddVertex(v => v.Add(pos));
            return this;
        }

        public static VertexBufferBuilder BuildPosUv()
        {
            var vd = new VertexDeclarationBuilder()
                .AddVertex3f()
                .AddVertex2f()
                .Build();
            return new VertexBufferBuilder(vd);
        }

        public VertexBufferBuilder Add(Vector3 pos, Vector2 uv)
        {
            AddVertex(v => v.Add(pos).Add(uv));
            return this;
        }
        public static VertexBufferBuilder BuildPosUvNorm()
        {
            var vd = new VertexDeclarationBuilder()
                .AddVertex3f()
                .AddVertex2f()
                .AddVertex3f()
                .Build();
            return new VertexBufferBuilder(vd);
        }

        public VertexBufferBuilder Add(Vector3 pos, Vector2 uv, Vector3 normal)
        {
            AddVertex(v => v.Add(pos).Add(uv).Add(normal));
            return this;
        }

        public static VertexBufferBuilder BuildPosUvNormTanBt()
        {
            var vd = new VertexDeclarationBuilder()
                .AddVertex3f()
                .AddVertex2f()
                .AddVertex3f()
                .AddVertex3f()
                .AddVertex3f()
                .Build();
            return new VertexBufferBuilder(vd);
        }

        public VertexBufferBuilder Add(
            Vector3 pos, Vector2 uv, Vector3 normal, Vector3 tangent, Vector3 bitangent)
        {
            AddVertex(v => v.Add(pos).Add(uv).Add(normal).Add(tangent).Add(bitangent));
            return this;
        }

        public static VertexBufferBuilder BuildPosUvNormTanBtBiBw()
        {
            var vd = new VertexDeclarationBuilder()
                .AddVertex3f()
                .AddVertex2f()
                .AddVertex3f()
                .AddVertex3f()
                .AddVertex3f()
                .AddVertex4i()
                .AddVertex4f()
                .Build();
            return new VertexBufferBuilder(vd);
        }
        public VertexBufferBuilder Add(
            Vector3 pos, Vector2 uv, Vector3 normal, Vector3 tangent, Vector3 bitangent, Vector4D<int> boneIDs, Vector4 boneWeights)
        {
            AddVertex(v => v.Add(pos).Add(uv).Add(normal).Add(tangent).Add(bitangent).Add(boneIDs).Add(boneWeights));
            return this;
        }

        public static VertexBufferBuilder BuildPosUvNormBiBw()
        {
            var vd = new VertexDeclarationBuilder()
                .AddVertex3f()
                .AddVertex2f()
                .AddVertex3f()
                .AddVertex4i()
                .AddVertex4f()
                .Build();
            return new VertexBufferBuilder(vd);
        }
        public VertexBufferBuilder Add(
            Vector3 pos, Vector2 uv, Vector3 normal, Vector4D<int> boneIDs, Vector4 boneWeights)
        {
            AddVertex(v => v.Add(pos).Add(uv).Add(normal).Add(boneIDs).Add(boneWeights));
            return this;
        }
        public byte[] Build()
        {
            return _memory.ToArray(); 
        }
    }
}
