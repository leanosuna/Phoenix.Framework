using Phoenix.Framework.Rendering.Geometry.Vertices;
using Silk.NET.OpenGL;

namespace Phoenix.Framework.Rendering.Geometry.Model.Meshes
{
    public class Mesh
    {
        internal PrimitiveType _type;
        internal DrawElementsType _drawElementsType;
        internal uint _VAHandle;
        internal uint _indicesLength;
        internal GL GL;
        public VertexDeclaration VertexDeclaration;

        public Mesh(GL gl) { 
            GL = gl;
            _type = PrimitiveType.Triangles;
            _drawElementsType = DrawElementsType.UnsignedInt;
        }

        public unsafe void Draw()
        {
            GL.BindVertexArray(_VAHandle);
            GL.DrawElements(_type, _indicesLength, _drawElementsType, null);
        }

    }
}
