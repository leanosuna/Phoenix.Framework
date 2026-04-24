using Phoenix.Framework.Rendering.GUI;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Phoenix.Framework.Rendering.Shaders
{
    public class ShaderUniform<Type>
    {
        int _location;
        private GLShader _shader;
        public ShaderUniform(GLShader shader, string name, bool throwIfNotFound = true)
        {
            _shader = shader;
            _location = shader.GetUniformLocation(name);
            if (_location == -1 && throwIfNotFound)
            {
                ErrorListWindow.Add($"Uniform [{name}] not found");
                return;
            }
        }

        public void Set(Type value)
        {
            _shader.SetUniform(_location, value);
        }
    }
}
