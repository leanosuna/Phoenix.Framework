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
        bool _notFound = false;
        public ShaderTextureUniform(GLShader shader, string name, int slot, bool throwIfNotFound = true)
        {
            _shader = shader;
            _slot = slot;
            _location = shader.GetUniformLocation(name);
            if (_location == -1)
            {
                _notFound = true;
                if (throwIfNotFound)
                    ErrorListWindow.Add($"Uniform [{name}] not found");
            }
        }

        public void Set(GLTexture tex)
        {
            if (_notFound) return;
            tex.Bind(TextureUnit.Texture0 + _slot);
            _shader.SetTextureUniform(_location, tex, _slot);
        }
        public void Set(GLTextureCube texCube)
        {
            if (_notFound) return;
            texCube.Bind(TextureUnit.Texture0 + _slot);
            _shader.SetTextureUniform(_location, texCube, _slot);
        }
        public void Set(uint tex)
        {
            if (_notFound) return;
            _shader.SetTextureUniform(_location, tex, _slot);
        }
    }
}
