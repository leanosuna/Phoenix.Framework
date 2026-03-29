using Silk.NET.OpenGL;
using System.Numerics;

namespace Phoenix.Rendering.RT
{
    public class RenderTarget
    {
        public string Name { get; private set; } = default!;
        public uint FrameBuffer
        {
            get;
            internal set;
        } = uint.MaxValue;
        internal bool IsBound => FrameBuffer != uint.MaxValue;
        public RenderTexture[] RenderTextures { get; internal set; } = default!;
        public int TexturesCount => RenderTextures.Length;

        private GL GL = default!;
        public DepthBuffer DepthBuffer { get; internal set; } = default!;
        internal RenderTarget(GL gl, string name, uint handle, RenderTexture[] targets)
        {
            Name = name;
            GL = gl;
            FrameBuffer = handle;
            RenderTextures = targets;
        }

        
        /// <summary>
        /// Copies the color buffer of a render target to the screen.
        /// </summary>

        public void CopyToScreen(int srcRTindex, Vector4 srcRect, Vector4 destRect, BlitFramebufferFilter filter = BlitFramebufferFilter.Nearest)
        {
            var maxTex = TexturesCount - 1;
            if (srcRTindex < 0 || srcRTindex > maxTex)
                throw new Exception($"Index {srcRTindex} out of bounds (0-{maxTex}) RT textures");

            var readBuffer = GLEnum.ColorAttachment0 + srcRTindex;

            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, FrameBuffer);
            GL.ReadBuffer(readBuffer);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
            GL.DrawBuffer(GLEnum.Front);

            GL.BlitFramebuffer((int)srcRect.X, (int)srcRect.Y, (int)srcRect.Z, (int)srcRect.W,
                (int)destRect.X, (int)destRect.Y, (int)destRect.Z, (int)destRect.W,
                ClearBufferMask.ColorBufferBit, filter);
        }
        /// <summary>
        /// Copies the selected color buffer of a render target to another.
        /// </summary>

        public void CopyTo((int RTindex, Vector4 Rect) src, (RenderTarget target, int RTindex, Vector4 Rect) dest, BlitFramebufferFilter filter = BlitFramebufferFilter.Nearest)
        {
            var maxTex = TexturesCount - 1;
            var maxTex2 = dest.target.TexturesCount - 1;

            if (src.RTindex < 0 || src.RTindex > maxTex)
                throw new Exception($"Index {src.RTindex} out of bounds (0-{maxTex}) RT textures");

            if (dest.RTindex < 0 || dest.RTindex > maxTex2)
                throw new Exception($"Index {dest.RTindex} out of bounds (0-{maxTex2}) RT textures");


            var readBuffer = GLEnum.ColorAttachment0 + src.RTindex;
            var drawBuffer = GLEnum.ColorAttachment0 + dest.RTindex;
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, FrameBuffer);
            GL.ReadBuffer(readBuffer);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, dest.target.FrameBuffer);
            GL.DrawBuffer(drawBuffer);

            GL.BlitFramebuffer((int)src.Rect.X, (int)src.Rect.Y, (int)src.Rect.Z, (int)src.Rect.W,
                (int)dest.Rect.X, (int)dest.Rect.Y, (int)dest.Rect.Z, (int)dest.Rect.W,
                ClearBufferMask.ColorBufferBit, filter);
        }


        public void CopyTo((int RTindex, Vector4 Rect) src, (RenderTarget target, int RTindex, Vector4 Rect)[] dest, BlitFramebufferFilter filter = BlitFramebufferFilter.Nearest)
        {
            var maxTex = TexturesCount - 1;
            //TODO: multiple texture copy
//If you want to blit into multiple attachments simultaneously, pass an array to glDrawBuffers(via Silk's GL.DrawBuffers) listing the attachments you want enabled. The blit will replicate the read buffer into each enabled draw buffer.

//If you also want to copy depth or stencil, bind both frames with proper attachments and include DepthBufferBit | StencilBufferBit in the mask(and ensure attachments exist).

            //if (src.RTindex < 0 || src.RTindex > maxTex)
            //    throw new Exception($"Index {src.RTindex} out of bounds (0-{maxTex}) RT textures");

            //var maxTex2 = dest.target.TexturesCount - 1;
            //if (dest.RTindex < 0 || dest.RTindex > maxTex2)
            //    throw new Exception($"Index {dest.RTindex} out of bounds (0-{maxTex2}) RT textures");


            //var readBuffer = GLEnum.ColorAttachment0 + src.RTindex;
            //var drawBuffer = GLEnum.ColorAttachment0 + dest.RTindex;
            //GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, FrameBuffer);
            //GL.ReadBuffer(readBuffer);
            //GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, dest.target.FrameBuffer);
            //GL.DrawBuffer(drawBuffer);

            //GL.BlitFramebuffer((int)src.Rect.X, (int)src.Rect.Y, (int)src.Rect.Z, (int)src.Rect.W,
            //    (int)dest.Rect.X, (int)dest.Rect.Y, (int)dest.Rect.Z, (int)dest.Rect.W,


        }
    }
}
