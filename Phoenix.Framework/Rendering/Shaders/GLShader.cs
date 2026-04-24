using Phoenix.Framework.Rendering.GUI;
using Phoenix.Framework.Rendering.Textures;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Phoenix.Framework.Rendering.Shaders
{
    public class GLShader : IDisposable
    {
        static uint _currentShader = uint.MaxValue;
        private uint _handle;
        private GL GL;
        private bool ignoreUniformsNotFound;

        private Dictionary<string, int> uniformLocations = new Dictionary<string, int>();

        public GLShader(GL glContext, string path, bool ignoreUniformsNotFound = false) :
            this(glContext, $"{path}.vert", $"{path}.frag", ignoreUniformsNotFound)
        {

        }
        public GLShader(GL glContext, string vertexPath, string fragmentPath, bool ignoreUniformsNotFound = false)
        {
            GL = glContext;

            uint vertex = LoadShader(ShaderType.VertexShader, vertexPath);
            uint fragment = LoadShader(ShaderType.FragmentShader, fragmentPath);

            _handle = GL.CreateProgram();
            GL.AttachShader(_handle, vertex);
            GL.AttachShader(_handle, fragment);
            GL.LinkProgram(_handle);
            GL.GetProgram(_handle, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                throw new Exception($"Program failed to link with error: {GL.GetProgramInfoLog(_handle)}");
            }

            GL.DetachShader(_handle, vertex);
            GL.DetachShader(_handle, fragment);
            GL.DeleteShader(vertex);
            GL.DeleteShader(fragment);
            this.ignoreUniformsNotFound = ignoreUniformsNotFound;
        }
        public void AttachUBO(uint bufferHandle, string uniformBlockName, uint binding = 0)
        {
            var index = GL.GetUniformBlockIndex(_handle, uniformBlockName);
            if(index == uint.MaxValue)
            {
                ErrorListWindow.Add($"Uniform block {uniformBlockName} not found");
                return;
            }    
            GL.UniformBlockBinding(_handle, index, binding);

            GL.BindBufferBase(BufferTargetARB.UniformBuffer, binding, bufferHandle);
        }
        public void SetAsCurrentGLProgram()
        {
            if(!IsCurrent())
            {
                _currentShader = _handle;
                GL.UseProgram(_handle);
            }
        }

        public bool IsCurrent()
        {
            return _currentShader == _handle;
        }

        

        public int GetUniformLocation(string name)
        {
            if (uniformLocations.TryGetValue(name, out int location))
                return location;

            location = GL.GetUniformLocation(_handle, name);

            if (location != -1)
                uniformLocations.Add(name, location);
            return location;

        }

        public unsafe void SetUniform<T>(int location, T value)
        {
            switch (value)
            {
                case bool b: GL.ProgramUniform1(_handle, location, b ? 1 : 0); break;
                case int i: GL.ProgramUniform1(_handle, location, i); break;
                case float f: GL.ProgramUniform1(_handle, location, f); break;
                case double d: GL.ProgramUniform1(_handle, location, d); break;
                case Vector2 v2: GL.ProgramUniform2(_handle, location, v2); break;
                case Vector3 v3: GL.ProgramUniform3(_handle, location, v3); break;
                case Vector4 v4: GL.ProgramUniform4(_handle, location, v4); break;

                case float[] fa:
                    fixed (float* ptr = fa)
                    {
                        GL.ProgramUniform1(_handle, location, (uint)fa.Length, ptr);
                    }
                    break;
                case Vector2[] v2a:
                    fixed (Vector2* ptr = v2a)
                    {
                        GL.ProgramUniform2(_handle, location, (uint)v2a.Length, (float*)ptr);
                    }
                    break;
                case Vector3[] v3a:
                    fixed (Vector3* ptr = v3a)
                    {
                        GL.ProgramUniform3(_handle, location, (uint)v3a.Length, (float*)ptr);
                    }
                    break;
                case Vector4[] v4a:
                    fixed (Vector4* ptr = v4a)
                    {
                        GL.ProgramUniform4(_handle, location, (uint)v4a.Length, (float*)ptr);
                    }
                    break;

                case Matrix4x4 m:
                    GL.ProgramUniformMatrix4(_handle, location, 1, false, (float*)&m); break;
                case Matrix4x4[] mm:
                    fixed (Matrix4x4* ptr = mm)
                    {
                        GL.ProgramUniformMatrix4(_handle, location, (uint)mm.Length, false, (float*)ptr);
                    }
                    break;
                default: 
                    ErrorListWindow.Add($"{typeof(T).Name} missing GL.UniformT entry");
                    return;
            }
        }
        public unsafe void SetUniform<T>(string name, T value)
        {
            int location = GetUniformLocation(name);
            if (location == -1)
            {
                if (ignoreUniformsNotFound)
                    return;
                ErrorListWindow.Add($"[{name}] not found on shader.");
                return;
            }
            SetUniform(location, value);
        }
        public void SetTextureUniform(string name, uint tex, int slot)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + slot);
            GL.BindTexture(TextureTarget.Texture2D, tex);
            SetUniform(name, slot);
        }
        public void SetTextureUniform(string name, GLTexture tex, int slot)
        {
            tex.Bind(TextureUnit.Texture0 + slot);
            SetUniform(name, slot);
        }

        public void SetTextureUniform(int location, uint tex, int slot)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + slot);
            GL.BindTexture(TextureTarget.Texture2D, tex);
            SetUniform(location, slot);
        }
        public void SetTextureUniform(int location, GLTexture tex, int slot)
        {
            tex.Bind(TextureUnit.Texture0 + slot);
            SetUniform(location, slot);
        }

        public void Dispose()
        {
            GL.DeleteProgram(_handle);
        }

        private uint LoadShader(ShaderType type, string path)
        {
            //string ver = ApiVersionInsert();
            string src = File.ReadAllText(path);
            uint handle = GL.CreateShader(type);
            GL.ShaderSource(handle, src);
            GL.CompileShader(handle);
            string infoLog = GL.GetShaderInfoLog(handle);
            if (!string.IsNullOrWhiteSpace(infoLog))
            {
                ErrorListWindow.Add($"Error compiling shader of type {type}, failed with error {infoLog}");
                return uint.MaxValue;
            }

            return handle;
        }
    }
}
