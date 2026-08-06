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
        bool _notFound = false;
        public ShaderUniform(GLShader shader, string name, bool throwIfNotFound = true)
        {
            _shader = shader;
            _location = shader.GetUniformLocation(name);
            if (_location == -1)
            {
                _notFound = true;
                if (throwIfNotFound)
                    ErrorListWindow.Add($"Uniform [{name}] not found");
            }
        }

        public void Set(Type value)
        {
            if (_notFound)
                return;
            _shader.SetUniform(_location, value);
        }
    }
}
