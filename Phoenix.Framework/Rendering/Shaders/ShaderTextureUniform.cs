using Phoenix.Framework.Rendering.GUI;
using Phoenix.Framework.Rendering.Textures;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.Framework.Rendering.Shaders
{
    public class ShaderTextureUniform
    {
        int _location;
        public GLShader _shader;
        int _slot;
        public ShaderTextureUniform(GLShader shader, string name, int slot, bool throwIfNotFound = true)
        {
            _shader = shader;
            _slot = slot;
            _location = shader.GetUniformLocation(name);
            if (_location == -1 && throwIfNotFound)
            {
                ErrorListWindow.Add($"Uniform [{name}] not found");
                return;
            }
        }

        public void Set(GLTexture tex)
        {
            tex.Bind(TextureUnit.Texture0 + _slot);
            _shader.SetTextureUniform(_location, tex, _slot);
        }
        public void Set(uint tex)
        {
            _shader.SetTextureUniform(_location, tex, _slot);
        }
    }
}
