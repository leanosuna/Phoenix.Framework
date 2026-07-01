using Phoenix.Framework.Rendering.Geometry.Vertices;
using Silk.NET.OpenGL;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Phoenix.Framework.Rendering.Geometry.Model.Meshes
{
    public unsafe class Mesh<IndexType>
        where IndexType : unmanaged
    {
        internal PrimitiveType _type;
        internal DrawElementsType _drawElementsType;
        internal uint _VAHandle;
        internal uint _indicesLength;
        internal GL GL;
        public VertexDeclaration VertexDeclaration;
        internal byte[]? _vertexData;
        internal IndexType[]? _indices;
        public Mesh(GL gl, PrimitiveType type, VertexDeclaration vertexDeclaration, IndexType[] indices, byte[] data, bool saveVertexData = false)
        {
            GL = gl;
            _type = type;
            VertexDeclaration = vertexDeclaration;

            if (indices.Length == 0)
                throw new Exception("indices not set");

            if (typeof(IndexType) == typeof(uint))
            {
                _drawElementsType = DrawElementsType.UnsignedInt;
            }
            else if (typeof(IndexType) == typeof(ushort))
            {
                _drawElementsType = DrawElementsType.UnsignedShort;
            }
            else if (typeof(IndexType) == typeof(byte))
            {
                _drawElementsType = DrawElementsType.UnsignedByte;
            }


            _VAHandle = gl.GenVertexArray();
            gl.BindVertexArray(_VAHandle);

            var VBHandle = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, VBHandle);

            var totalBytes = data.Length;

            if (totalBytes == 0)
                throw new Exception("data not set");


            fixed (byte* p = data)
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)totalBytes, p, BufferUsageARB.StaticDraw);
            }

            var IBHandle = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, IBHandle);

            fixed (void* i = indices)
            {
                gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)), i, BufferUsageARB.StaticDraw);
            }

            int attributeByteOffset = 0;
            var vd = vertexDeclaration;

            for (var i = 0; i < vd.VertexItems.Count; i++)
            {
                var item = vd.VertexItems[i];

                if (item.IsInt)
                {
                    gl.VertexAttribIPointer(
                        (uint)i,
                        item.Size,
                        VertexAttribIType.Int,
                        (uint)vd.StrideBytes,
                        (void*)attributeByteOffset);
                }
                else
                {
                    gl.VertexAttribPointer(
                        (uint)i,
                        item.Size,
                        VertexAttribPointerType.Float,
                        false,
                        (uint)vd.StrideBytes,
                        (void*)attributeByteOffset);
                }

                attributeByteOffset += item.SizeBytes;
                gl.EnableVertexAttribArray((uint)i);

            }

            _indicesLength = (uint)indices.Length;

            if (saveVertexData)
            {
                _vertexData = data;
                _indices = indices;
            }
        }

        public void Draw()
        {
            GL.BindVertexArray(_VAHandle);
            GL.DrawElements(_type, _indicesLength, _drawElementsType, null);
        }

        public T[] GetVertexData<T>() where T : unmanaged
        {
            if (_vertexData == null)
                throw new InvalidOperationException("Vertex data was not saved. Set saveVertexData to true when creating the mesh.");

            var stride = Unsafe.SizeOf<T>();
            if (stride != VertexDeclaration.StrideBytes)
                throw new InvalidOperationException($"Type stride {stride} does not match vertex declaration stride {VertexDeclaration.StrideBytes}.");

            var count = _vertexData.Length / stride;
            var result = new T[count];
            MemoryMarshal.Cast<byte, T>(_vertexData.AsSpan()).CopyTo(result);
            return result;
        }

        public IndexType[] GetIndexData()
        {
            if(_indices == null)
                throw new InvalidOperationException("Index data was not saved. Set saveVertexData to true when creating the mesh.");

            return _indices;
        }
    }
}
