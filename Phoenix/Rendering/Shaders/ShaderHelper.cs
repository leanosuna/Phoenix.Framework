
namespace Phoenix.Rendering.Shaders
{
    public abstract partial class ShaderHelper
    {
        protected GLShader _shader = default!;
        public void Use()
        {
            _shader.SetAsCurrentGLProgram();
        }
        public void AttachUBO(uint bufferHandle, string uniformBlockName, uint binding = 0)
        {
            _shader.AttachUBO(bufferHandle, uniformBlockName, binding);
        }
        public void Dispose()
        {
            _shader.Dispose();
        }

    }
}
