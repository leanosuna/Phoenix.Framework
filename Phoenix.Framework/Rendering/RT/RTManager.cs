using Phoenix.Framework.Rendering.Textures;
using Silk.NET.OpenGL;
using System.Globalization;
using System.Numerics;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace Phoenix.Framework.Rendering.RT
{
    public class RTManager
    {
        private GL GL;
        private List<RenderTarget> _dynamicTargets = new List<RenderTarget>();
        private Dictionary<string, RenderTarget> _renderTargets = new Dictionary<string, RenderTarget>();
        private PhoenixGame _game;
        private int _rtGenNameCount = 0;
        internal RTManager(PhoenixGame game)
        {
            _game = game;
            GL = game.GL;
        }

        internal RTBuilder BuildRT()
        {
            return new RTBuilder(this);
        }
        internal RTTBuilder BuildRTT()
        {
            return new RTTBuilder(GL);
        }
        private string GenName() 
        {
            var name = $"rt-{_rtGenNameCount}";
            _rtGenNameCount ++;

            return name;
        }
        
        internal RenderTarget BuildDefault()
        {
            return BuildRT()
                .AddTexture(new RenderTexture(GL))
                .SetDepthBuffer(new DepthBuffer())
                .Build();
        }


        internal RenderTarget CreateRenderTarget(RenderTargetInfo info)
        {
            var name = info.Name;
            if(string.IsNullOrEmpty(name))
                name = GenName();
            if (_renderTargets.ContainsKey(name))
                throw new Exception($"targets cant have the same name {name}");
            

            var targetTextures = info.Textures;
            if(targetTextures.Count == 0) 
                targetTextures.Add(BuildRTT().Build());

            var depthBuffer = info.DepthBuffer;

            GL.GenFramebuffers(1, out uint rtFrameBuffer);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, rtFrameBuffer);

            var targetCount = (uint)targetTextures.Count;

            CreateRenderTextures(targetTextures);

            var rtSize = targetTextures[0].Texture.Size;

            if (depthBuffer is not null)
            {
                if (depthBuffer.FollowsWindowSize)
                    depthBuffer.Size = _game.WindowSize;

                GL.GenRenderbuffers(1, out uint rboDepth);
                depthBuffer.Handle = rboDepth;
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rboDepth);
                //TODO: verify if depthBuffer size necessary
                GL.RenderbufferStorage(GLEnum.Renderbuffer, depthBuffer.Format, (uint)rtSize.X, (uint)rtSize.Y);

                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, rboDepth);

            }
            var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != GLEnum.FramebufferComplete)
                throw new Exception($"Framebuffer incomplete: {status}");

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);


            var rt = new RenderTarget(GL, name, rtFrameBuffer, targetTextures.ToArray());

            rt.DepthBuffer = depthBuffer!;
            _renderTargets.Add(name.ToLower(CultureInfo.InvariantCulture), rt);

            if (targetTextures.Any(t => t.FollowsWindowSize))
                _dynamicTargets.Add(rt);


            return rt;
        }


        internal RenderTarget FindByName(string name)
        {
            if (!_renderTargets.TryGetValue(name.ToLower(CultureInfo.InvariantCulture), out var rt))
                throw new Exception($"RT {name} not found");    
            return rt;
        }
        internal bool FindByName(string name, out RenderTarget target)
        {
            return _renderTargets.TryGetValue(name.ToLower(CultureInfo.InvariantCulture), out target!);
        }

        private unsafe void CreateRenderTextures(List<RenderTexture> targetTextures)
        {
            uint targetCount = (uint)targetTextures.Count();
            
            GLEnum[] attachments = new GLEnum[targetCount];
            for (int i = 0; i < targetCount; i++)
            {
                var tex = targetTextures[i];

                var size = tex.Size;
                if (tex.FollowsWindowSize)
                    size = _game.WindowSize;

                tex.Texture.Resize(size);
                
                var att = FramebufferAttachment.ColorAttachment0 + i;
                var cAtt = GLEnum.ColorAttachment0 + i;
                attachments[i] = cAtt;
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                    att, TextureTarget.Texture2D, tex.Texture.Handle, 0);

            }
            GL.DrawBuffers(targetCount, attachments);
        }

        /// <summary>
        /// Selects a render target as GL output 
        /// </summary>
        /// <param name="name">The name of the render target to bind</param>
        internal void RenderTo(RenderTarget target)
        {
            if(!target.IsBound)
                throw new Exception($"target not bound (its handle is uint.MaxValue)");

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, target.FrameBuffer);

            var size = target.RenderTextures[0].Texture.Size;
            GL.Viewport(0, 0, (uint)size.X, (uint)size.Y);
        }
        /// <summary>
        /// Selects the screen rt as GL output 
        /// </summary>
        /// <param name="name">The name of the render target to bind</param>
        internal void RenderToScreen()
        {
            RenderTo(_game._sceneRT);
        }

        /// <summary>
        /// Selects the screen as GL output 
        /// </summary>
        /// <param name="name">The name of the render target to bind</param>
        internal void TrueRenderToScreen()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, (uint)_game.WindowSize.X, (uint)_game.WindowSize.Y);
        }

        internal void HandleWindowResize()
        {
            foreach(var rt in _dynamicTargets)
            {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, rt.FrameBuffer);

                foreach (var tex in rt.RenderTextures)
                {
                    if(tex.FollowsWindowSize)
                    {
                        var size = _game.Graphics.RenderViewport.Size * tex.SizeMultiplier;
                        tex.Texture.Resize(size);
                    }

                }
                
                var db = rt.DepthBuffer;
                if (db is not null)
                {
                    if(db.FollowsWindowSize)
                    {
                        
                        
                        db.Size = _game.Graphics.RenderViewport.Size;
                        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, db.Handle);
                        GL.RenderbufferStorage(
                            RenderbufferTarget.Renderbuffer, 
                            db.Format,
                            db.Width, 
                            db.Height);

                        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
                    }
                }
            }
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }
    }
}
