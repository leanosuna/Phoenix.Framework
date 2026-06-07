using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.Framework.Rendering.Geometry.Vertices
{
    internal static class VertexDeclarationHelper
    {
        internal static bool IsInt(this VertexAttributeType type)
        {
            return type switch
            {
                VertexAttributeType.Float => false,
                VertexAttributeType.Int => true,
                VertexAttributeType.Vertex2 => false,
                VertexAttributeType.Vertex2i => true,
                VertexAttributeType.Vertex3 => false,
                VertexAttributeType.Vertex3i => true,
                VertexAttributeType.Vertex4 => false,
                VertexAttributeType.Vertex4i => true,

                _ => false
            };
        }
        internal static int SizeInBytes(this VertexAttributeType type)
        {
            return type switch
            {
                VertexAttributeType.Float => sizeof(float),
                VertexAttributeType.Int => sizeof(int),
                VertexAttributeType.Vertex2 => sizeof(float) * 2,
                VertexAttributeType.Vertex2i => sizeof(int) * 2,
                VertexAttributeType.Vertex3 => sizeof(float) * 3,
                VertexAttributeType.Vertex3i => sizeof(int) * 3,
                VertexAttributeType.Vertex4 => sizeof(float) * 4,
                VertexAttributeType.Vertex4i => sizeof(int) * 4,

                _ => sizeof(float)
            };
        }
        internal static int Size(this VertexAttributeType type)
        {
            return type switch
            {
                VertexAttributeType.Float => 1,
                VertexAttributeType.Int => 1,
                VertexAttributeType.Vertex2 => 2,
                VertexAttributeType.Vertex2i => 2,
                VertexAttributeType.Vertex3 => 3,
                VertexAttributeType.Vertex3i => 3,
                VertexAttributeType.Vertex4 => 4,
                VertexAttributeType.Vertex4i => 4,

                _ => sizeof(float)
            };
        }
    }
}
