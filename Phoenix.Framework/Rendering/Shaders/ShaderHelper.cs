
namespace Phoenix.Framework.Rendering.Shaders
{
    public abstract partial class ShaderHelper
    {
        protected GLShader _shader = null!;

        private GLShader EnsureShader()
        {
            if (_shader is null)
                throw new InvalidOperationException(
                    $"ShaderHelper of type {GetType().Name} has no _shader. " +
                    "Derived classes must assign _shader in their constructor.");

            return _shader;
        }

        public void Use()
        {
            EnsureShader().SetAsCurrentGLProgram();
        }
        public void AttachUBO(uint bufferHandle, string uniformBlockName, uint binding = 0)
        {
            EnsureShader().AttachUBO(bufferHandle, uniformBlockName, binding);
        }
        public void Dispose()
        {
            EnsureShader().Dispose();
        }

    }
}
