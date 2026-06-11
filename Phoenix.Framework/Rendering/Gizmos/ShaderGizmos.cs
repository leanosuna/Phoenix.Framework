using Phoenix.Framework.Rendering.Shaders;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Phoenix.Framework.Rendering.Gizmos
{
    internal class ShaderGizmos : ShaderHelper
    {
        public ShaderUniform<Matrix4x4> World;
        public ShaderUniform<Vector3> Color;
        public ShaderUniform<bool> Hit;
        
        public ShaderGizmos(GL gl)
        {

            _shader = new GLShader(gl,
                EmbeddedHelper.ExtractPath("gizmos.vert", "Files.Shaders.gizmos"),
                EmbeddedHelper.ExtractPath("gizmos.frag", "Files.Shaders.gizmos"));
            
            World = new ShaderUniform<Matrix4x4>(_shader, "uWorld");
            Color = new ShaderUniform<Vector3>(_shader, "uColor");
            Hit = new ShaderUniform<bool>(_shader, "uHit");
        }
    }
}
