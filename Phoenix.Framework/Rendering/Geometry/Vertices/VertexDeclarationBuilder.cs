using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.Framework.Rendering.Geometry.Vertices
{
    public class VertexDeclarationBuilder
    {

        private List<VertexAttributeType> _declaration = new();
        public VertexDeclarationBuilder()
        {
        }

        public VertexDeclarationBuilder AddFloat()
        {
            _declaration.Add(VertexAttributeType.Float);
            return this;
        }
        public VertexDeclarationBuilder AddInt()
        {
            _declaration.Add(VertexAttributeType.Int);
            return this;
        }
        public VertexDeclarationBuilder AddVertex2f()
        {
            _declaration.Add(VertexAttributeType.Vertex2);
            return this;
        }
        public VertexDeclarationBuilder AddVertex2i()
        {
            _declaration.Add(VertexAttributeType.Vertex2i);
            return this;
        }
        public VertexDeclarationBuilder AddVertex3f()
        {
            _declaration.Add(VertexAttributeType.Vertex3);
            return this;
        }
        public VertexDeclarationBuilder AddVertex3i()
        {
            _declaration.Add(VertexAttributeType.Vertex3i);
            return this;
        }
        public VertexDeclarationBuilder AddVertex4f()
        {
            _declaration.Add(VertexAttributeType.Vertex4);
            return this;
        }
        public VertexDeclarationBuilder AddVertex4i()
        {
            _declaration.Add(VertexAttributeType.Vertex4i);
            return this;
        }
        public VertexDeclaration Build()
        {
            return new VertexDeclaration(_declaration);
        }

    }
}
