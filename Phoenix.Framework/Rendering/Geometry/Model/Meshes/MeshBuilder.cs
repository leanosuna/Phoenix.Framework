using Phoenix.Framework.Rendering.Geometry.Vertices;
using Silk.NET.Core.Native;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Phoenix.Framework.Rendering.Geometry.Model.Meshes
{
    public unsafe class MeshBuilder <IndexType>
        where IndexType : unmanaged
        
    {
        private GL GL;

        private Mesh _mesh;
        private uint _VBHandle;
        private uint _IBHandle;

        private IndexType[] _indices = [];
        
        private byte[] _data = [];

        public MeshBuilder(GL gl)
        {
            GL = gl;
            _mesh = new Mesh(gl);
        }

        public MeshBuilder<IndexType> SetDrawType(PrimitiveType type)
        {
            _mesh._type = type;

            return this;
        }

        public MeshBuilder<IndexType> SetVertexDeclaration(VertexDeclaration vertexDeclaration)
        {
            _mesh.VertexDeclaration = vertexDeclaration;
            return this;
        }

        public MeshBuilder<IndexType> SetVertexData<VertexType>(VertexType[] vertices)
            where VertexType : unmanaged
        {
            
            _data = MemoryMarshal.AsBytes(vertices).ToArray();
            return this;
        }

        public MeshBuilder<IndexType> SetVertexData(byte[] data)
        {
            _data = data;
            return this;
        }

        public MeshBuilder<IndexType> SetIndices(IndexType[] indices)
        {
            _indices = indices;
            _mesh._indicesLength = (uint)_indices.Length;
            return this;
        }
       
        public Mesh Build()
        {
            if (_mesh._indicesLength == 0)
                throw new Exception("indices not set");

            if (typeof(IndexType) == typeof(uint))
            {
                _mesh._drawElementsType = DrawElementsType.UnsignedInt;
            }
            else if (typeof(IndexType) == typeof(ushort))
            {
                _mesh._drawElementsType = DrawElementsType.UnsignedShort;
            }
            else if (typeof(IndexType) == typeof(byte))
            {
                _mesh._drawElementsType = DrawElementsType.UnsignedByte;
            }

            Log.Debug($"element type {_mesh._drawElementsType.ToString()}");

            _mesh._VAHandle = GL.GenVertexArray();
            GL.BindVertexArray(_mesh._VAHandle);

            _VBHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, _VBHandle);

            //set data 
            var totalBytes = _data.Length;
            Log.Debug($"data length {totalBytes}");

            if (totalBytes == 0)
                throw new Exception("data not set");

            
            fixed (byte* p = _data)
            {
                GL.BufferData(BufferTargetARB.ArrayBuffer, (nuint)totalBytes, p, BufferUsageARB.StaticDraw);
            }

            _IBHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, _IBHandle);

            fixed (void* i = _indices)
            {
                GL.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(_indices.Length * sizeof(uint)), i, BufferUsageARB.StaticDraw);
            }

            int attributeByteOffset = 0;
            var vd = _mesh.VertexDeclaration;
            
            for(var i = 0; i < vd.VertexItems.Count; i++)
            {
                var item = vd.VertexItems[i];

                if (item.IsInt)
                {
                    GL.VertexAttribIPointer(
                        (uint)i,
                        item.Size,
                        VertexAttribIType.Int,
                        (uint)vd.StrideBytes,
                        (void*)attributeByteOffset);
                }
                else
                {
                    GL.VertexAttribPointer(
                        (uint)i,
                        item.Size,
                        VertexAttribPointerType.Float,
                        false,
                        (uint)vd.StrideBytes,
                        (void*)attributeByteOffset);
                }

                attributeByteOffset += item.SizeBytes;
                GL.EnableVertexAttribArray((uint)i);

                var typestr = item.IsInt ? "Int":"Float";
                Log.Debug($"item {i} size {item.Size} type {typestr} stride {vd.StrideBytes}");

            }

            Log.Debug($"final offset {attributeByteOffset}");





            return _mesh;
        }
    }
}




