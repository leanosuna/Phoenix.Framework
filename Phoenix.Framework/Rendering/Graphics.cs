using Phoenix.Framework.Rendering.GUI;
using Phoenix.Framework.Rendering.RT;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Text;
using System.Xml.Linq;

namespace Phoenix.Framework.Rendering
{
    public class Graphics
    {
        private PhoenixGame _game;
        private GL GL;
        private IWindow _window;
        private RTManager _rtManager;

        public double Time { get; internal set; } = 0;
        public double FrameTime { get; internal set; } = 0;
        public double FT_SAMPLE { get; internal set; } = 0;
        public double FT_SAMPLE_RATE { get; set; } = 0.3;
        public double FPS { get; internal set; } = 0;
        public double FPS_SAMPLE { get; internal set; } = 0;
        public double FPS_SAMPLE_RATE { get; set; } = 0.3;

        public Key RenderHaltKey { get; set; } = Key.F11;

        public RenderViewport RenderViewport { get; private set; }

        public Graphics(PhoenixGame game)
        {
            _game = game;
            GL = _game.GL;
            _window = _game.Window;
            _rtManager = _game.RTManager;
            RenderViewport = new RenderViewport(game);
        }
        #region Window
        public void SetResolution(Vector2 size, bool fullscreen = true)
        {
            if (fullscreen)
            {
                _window.WindowState = WindowState.Fullscreen;
            }
            else
            {
                _window.WindowState = WindowState.Normal;
            }
            _window.Position = Vector2D<int>.Zero;
            _window.Size = size.To2Di();
        }
        public void SetResolution(Vector2 size, Vector2 position, bool fullscreen = true)
        {
            if (fullscreen)
            {
                _window.WindowState = WindowState.Fullscreen;
            }
            else
            {
                _window.WindowState = WindowState.Normal;
            }
            _window.Position = position.To2Di();
            _window.Size = size.To2Di();
        }
        public void SetWindowBorder(WindowBorder type)
        {
            _window.WindowBorder = type;
        }
        #endregion

        #region GL CFG
        private (bool, GLEnum) _depth = default!;
        public void SetDepthTest(bool enable, GLEnum type = GLEnum.Less)
        {
            if (_depth == (enable, type))
                return;
            _depth = (enable, type);
            
            if (enable)
            {
                GL.Enable(EnableCap.DepthTest);
                GL.DepthFunc(type);
            }
            else
            {
                GL.Disable(EnableCap.DepthTest);
            }

            
        }
        private (bool, BlendingFactor, BlendingFactor) _blend = default!;
        public void SetAlphaBlend(bool enable, BlendingFactor source = BlendingFactor.SrcAlpha, BlendingFactor destination = BlendingFactor.One)
        {
            if (_blend == (enable, source, destination))
                return;
            _blend = (enable, source, destination);

            if (enable)
            {
                GL.BlendFunc(source, destination);
                GL.Enable(GLEnum.Blend);
            }
            else
            {
                GL.Disable(GLEnum.Blend);
            }
        }
        private (bool, GLEnum, bool) _cull = default!;
        public void SetFaceCulling(bool enable, GLEnum face = GLEnum.Back, bool frontIsCcw = true)
        {
            if (_cull == (enable, face, frontIsCcw))
                return;
            _cull = (enable, face, frontIsCcw);
            
            if (enable)
            {
                GL.Enable(GLEnum.CullFace);
                GL.CullFace(face);
                GL.FrontFace(frontIsCcw ? GLEnum.Ccw : GLEnum.CW);
            }
            else
            {
                GL.Disable(GLEnum.CullFace);
            }
        }
        #endregion

        #region Render Targets

        public void ClearRenderTarget(ClearBufferMask mask = ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit)
        {
            GL.Clear(mask);
        }
        public void ClearRenderTarget(bool color, bool depth, bool stencil)
        {
            var mask = color ? ClearBufferMask.ColorBufferBit : ClearBufferMask.None;
            mask |= depth ? ClearBufferMask.DepthBufferBit : ClearBufferMask.None;
            mask |= stencil ? ClearBufferMask.StencilBufferBit : ClearBufferMask.None;

            GL.Clear(mask);
        }
        public void SetClearColor(Vector4 color)
        {
            GL.ClearColor(color.X, color.Y, color.Z, color.W);
        }
        public void SetClearColor(Color color)
        {
            GL.ClearColor(color);
        }
        public void SetRenderToTarget(RenderTarget target)
        {
            _rtManager.RenderTo(target);
        }
        public void SetRenderToScreen()
        {
            _rtManager.RenderToScreen();
        }

        public RTBuilder BuildRenderTarget() => _rtManager.BuildRT();
        public RTTBuilder BuildTargetTexture() => _rtManager.BuildRTT();
        public RenderTarget NewDefaultRenderTarget() => _rtManager.BuildDefault();
        public bool TryFindByName(string name, out RenderTarget target) => _rtManager.FindByName(name, out target);


        /// <summary>
        /// Copies the color buffer of a render target to the screen.
        /// </summary>

        internal void CopyToScreen(
            RenderTarget rt, int srcRTindex, Vector4 srcRect, 
            Vector4 destRect, BlitFramebufferFilter filter = BlitFramebufferFilter.Nearest)
        {
            CopyTo((rt, srcRTindex, srcRect), (_game._sceneRT, 0, new Vector4(0, 0, _game.WindowWidth, _game.WindowHeight)), filter);
        }

        /// <summary>
        /// Copies the color buffer of a render target to the screen rt.
        /// </summary>

        internal void TrueCopyToScreen(
            RenderTarget rt, int srcRTindex, Vector4 srcRect, 
            Vector4 destRect, BlitFramebufferFilter filter = BlitFramebufferFilter.Nearest)
        {
            var maxTex = rt.TexturesCount - 1;
            if (srcRTindex < 0 || srcRTindex > maxTex)
            {
                ErrorListWindow.Add($"Index {srcRTindex} out of bounds (0-{maxTex}) RT textures");
                return;
            }

            var readBuffer = GLEnum.ColorAttachment0 + srcRTindex;

            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, rt.FrameBuffer);
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

        public void CopyTo(
            (RenderTarget target, int RTindex, Vector4 Rect) src, 
            (RenderTarget target, int RTindex, Vector4 Rect) dest, 
            BlitFramebufferFilter filter = BlitFramebufferFilter.Nearest)
        {
            var maxTex = src.target.TexturesCount - 1;
            var maxTex2 = dest.target.TexturesCount - 1;

            if (src.RTindex < 0 || src.RTindex > maxTex)
            {
                ErrorListWindow.Add($"Index {src.RTindex} out of bounds (0-{maxTex}) RT textures");
                return;
            }    
            
            if (dest.RTindex < 0 || dest.RTindex > maxTex2)
            {
                ErrorListWindow.Add($"Index {dest.RTindex} out of bounds (0-{maxTex2}) RT textures");
                return;
            }
            
            var readBuffer = GLEnum.ColorAttachment0 + src.RTindex;
            var drawBuffer = GLEnum.ColorAttachment0 + dest.RTindex;
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, src.target.FrameBuffer);
            GL.ReadBuffer(readBuffer);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, dest.target.FrameBuffer);
            GL.DrawBuffer(drawBuffer);

            GL.BlitFramebuffer((int)src.Rect.X, (int)src.Rect.Y, (int)src.Rect.Z, (int)src.Rect.W,
                (int)dest.Rect.X, (int)dest.Rect.Y, (int)dest.Rect.Z, (int)dest.Rect.W,
                ClearBufferMask.ColorBufferBit, filter);
        }


        #endregion

    }
}
