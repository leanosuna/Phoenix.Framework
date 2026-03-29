using Phoenix.AssetImport;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Phoenix.Rendering.Geometry.Model
{
    public class ModelMesh
    {
        public Matrix4x4 Transform { get; internal set; }
        public string Name { get; internal set; } = string.Empty;
        
        BufferObject<uint> EBO = default!;
        uint _VAHandle;
        uint _VBhandle;

        private uint _indicesLength;
        GL GL;
        private MeshAttributes _attributes;
        public unsafe ModelMesh(GL gl, string name, Vertex[] vertices, uint[] indices, Matrix4x4 transform, bool isAnimated)
        {
            GL = gl;
            Name = name;
            Transform = transform;

            _indicesLength = (uint)indices.Length;

            _VAHandle = GL.GenVertexArray();
            GL.BindVertexArray(_VAHandle);
            _attributes =   
                isAnimated?
                MeshAttributes.Position3D |
                MeshAttributes.TexCoord |
                MeshAttributes.Normals | 
                MeshAttributes.boneIds |
                MeshAttributes.boneWeights
                :
                MeshAttributes.Position3D |
                MeshAttributes.TexCoord |
                MeshAttributes.Normals;

            _VBhandle = GL.GenBuffer();
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, _VBhandle);

            var attdata = CalculateAttributeData();
            (var data, var totalBytes) = PushData(vertices, attdata.strideBytes, attdata.layout);
            fixed (byte* p = data)
            {
                GL.BufferData(BufferTargetARB.ArrayBuffer, (nuint)totalBytes, p, BufferUsageARB.StaticDraw);
            }

            EBO = new BufferObject<uint>(GL, indices, BufferTargetARB.ElementArrayBuffer);
            EBO.Bind();

            SetVAOAttributes(attdata);

            GL.BindVertexArray(0);
        }

        public unsafe void Draw()
        {
            //if (_shader != default!)
            //    if (!_shader.IsCurrent() && _shouldThrowIfNotCurrent)
            //        throw new Exception($"Linked shader for this mesh is not the current one.");

            //bind VAO
            GL.BindVertexArray(_VAHandle);
            GL.DrawElements(PrimitiveType.Triangles, _indicesLength, DrawElementsType.UnsignedInt, null);
        }


        unsafe void SetVAOAttributes((int strideBytes, List<(MeshAttributes attr, int components, int bytes, bool isInt)> layout) attdata)
        {
            int attributeByteOffset = 0;
            for (uint attribIndex = 0; attribIndex < attdata.layout.Count; attribIndex++)
            {
                var entry = attdata.layout[(int)attribIndex];
                if (entry.isInt)
                {
                    GL.VertexAttribIPointer(attribIndex,
                                            entry.components,
                                            VertexAttribIType.Int,
                                            (uint)attdata.strideBytes,
                                            (void*)attributeByteOffset);
                }
                else
                {
                    GL.VertexAttribPointer(attribIndex,
                                           entry.components,
                                           VertexAttribPointerType.Float,
                                           false,
                                           (uint)attdata.strideBytes,
                                           (void*)attributeByteOffset);
                }
                GL.EnableVertexAttribArray(attribIndex);
                attributeByteOffset += entry.bytes;
            }

        }

        private (byte[], int) PushData(Vertex[] vertices, int strideBytes,
            List<(MeshAttributes attr, int components, int bytes, bool isInt)> layout)
        {
            var vertexCount = vertices.Length;
            var totalBytes = strideBytes * vertexCount;
            var data = new byte[totalBytes];
            int byteOffset = 0;
            for (int vi = 0; vi < vertexCount; vi++)
            {
                var v = vertices[vi];

                foreach (var entry in layout)
                {
                    switch (entry.attr)
                    {
                        case MeshAttributes.Position3D:
                            PushFloatArray([v.Position.X, v.Position.Y, v.Position.Z], data, ref byteOffset);
                            break;
                        case MeshAttributes.Position2D:
                            PushFloatArray([v.Position.X, v.Position.Y], data, ref byteOffset);
                            break;
                        case MeshAttributes.TexCoord:
                            PushFloatArray([v.TexCoords.X, v.TexCoords.Y], data, ref byteOffset);
                            break;
                        case MeshAttributes.Normals:
                            PushFloatArray([v.Normal.X, v.Normal.Y, v.Normal.Z], data, ref byteOffset);
                            break;
                        case MeshAttributes.Tangents:
                            PushFloatArray([v.Tangent.X, v.Tangent.Y, v.Tangent.Z], data, ref byteOffset);
                            break;
                        case MeshAttributes.Bitangents:
                            PushFloatArray([v.Bitangent.X, v.Bitangent.Y, v.Bitangent.Z], data, ref byteOffset);
                            break;
                        case MeshAttributes.boneIds:
                            PushIntArray([(int)v.BoneIds.X, (int)v.BoneIds.Y, (int)v.BoneIds.Z, (int)v.BoneIds.W], data, ref byteOffset); // int[]
                            break;
                        case MeshAttributes.boneWeights:
                            PushFloatArray([v.Weights.X, v.Weights.Y, v.Weights.Z, v.Weights.W], data, ref byteOffset); // float[]
                            break;
                        default:
                            throw new NotSupportedException(entry.attr.ToString());
                    }
                }
            }

            if (byteOffset != totalBytes)
                throw new Exception($"Buffer fill mismatch ({byteOffset} != {totalBytes})");

            return (data, totalBytes);
        }

        private (int strideBytes, List<(MeshAttributes attr, int components, int bytes, bool isInt)> layout)
            CalculateAttributeData()
        {
            int strideBytes = 0;
            var layout = new List<(MeshAttributes attr, int components, int bytes, bool isInt)>();
            foreach (MeshAttributes a in Enum.GetValues(typeof(MeshAttributes)))
            {
                if (_attributes.HasFlag(a))
                {
                    int bytes = SizeOfAttribute(a);
                    int comps = ItemCountOfAttribute(a);
                    bool isInt = (a == MeshAttributes.boneIds);
                    layout.Add((a, comps, bytes, isInt));
                    strideBytes += bytes;
                }
            }
            return (strideBytes, layout);
        }
        private void PushFloatArray(float[] src, byte[] data, ref int byteOffset)
        {
            var byteCount = src.Length * sizeof(float);
            System.Buffer.BlockCopy(src, 0, data, byteOffset, byteCount);
            byteOffset += byteCount;
        }
        private void PushIntArray(int[] src, byte[] data, ref int byteOffset)
        {
            //Console.WriteLine($"Pushing int array of length {src.Length} at offset {byteOffset}");
            var byteCount = src.Length * sizeof(int);
            System.Buffer.BlockCopy(src, 0, data, byteOffset, byteCount);
            byteOffset += byteCount;
        }

        public static int ItemCountOfAttribute(MeshAttributes attr)
        {
            switch (attr)
            {
                case MeshAttributes.Position2D:
                case MeshAttributes.TexCoord:
                    return 2;
                case MeshAttributes.Position3D:
                case MeshAttributes.Normals:
                case MeshAttributes.Tangents:
                case MeshAttributes.Bitangents:
                    return 3;
                case MeshAttributes.boneIds:
                    return 4;
                case MeshAttributes.boneWeights:
                    return 4;
                default:
                    return -1;
            }
        }

        public static int SizeOfAttribute(MeshAttributes attr)
        {
            switch (attr)
            {
                case MeshAttributes.Position2D:
                case MeshAttributes.TexCoord:
                    return 2 * sizeof(float);
                case MeshAttributes.Position3D:
                case MeshAttributes.Normals:
                case MeshAttributes.Tangents:
                case MeshAttributes.Bitangents:
                    return 3 * sizeof(float);
                case MeshAttributes.boneIds:
                    return 4 * sizeof(int);
                case MeshAttributes.boneWeights:
                    return 4 * sizeof(float);

                default:
                    return -1;
            }
        }


        public static VertexAttribPointerType TypeOfAttribute(MeshAttributes attr)
        {
            switch (attr)
            {
                case MeshAttributes.Position2D:
                case MeshAttributes.Position3D:
                case MeshAttributes.TexCoord:
                case MeshAttributes.Normals:
                case MeshAttributes.Tangents:
                case MeshAttributes.Bitangents:
                case MeshAttributes.boneWeights:
                    return VertexAttribPointerType.Float;

                case MeshAttributes.boneIds:
                    return VertexAttribPointerType.Int;
                default:
                    return VertexAttribPointerType.Float;
            }
        }

    }

    [Flags]
    public enum MeshAttributes
    {
        Position2D = 1 << 0,    // 0001
        Position3D = 1 << 1,    // 0010
        TexCoord = 1 << 2,      // 0100
        Normals = 1 << 3,       // 1000
        Tangents = 1 << 4,      // 10000
        Bitangents = 1 << 5,    // 100000
        boneIds = 1 << 6,       // 1000000
        boneWeights = 1 << 7    //10000000
    }
}
