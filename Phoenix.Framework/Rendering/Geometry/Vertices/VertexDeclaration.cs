using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.Framework.Rendering.Geometry.Vertices
{
    public class VertexDeclaration
    {
        public List<VertexDeclarationItem> VertexItems;
        public int StrideBytes { get; private set; }
        internal VertexDeclaration(List<VertexAttributeType> declaration)
        {
            VertexItems = declaration.Select(d => new VertexDeclarationItem(d)).ToList();

            StrideBytes = declaration.Sum(VertexDeclarationHelper.SizeInBytes);
        }

    }
}
