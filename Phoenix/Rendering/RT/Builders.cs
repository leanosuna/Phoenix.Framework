
using Phoenix.Rendering.Textures;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Phoenix.Rendering.RT
{
    public class RTTBuilder
    {
        private RenderTextureInfo _ti = new();
        private GL GL;
        public RTTBuilder(GL gl)
        {
            GL = gl;
        }
        public RTTBuilder SetFormat(InternalFormat format)
        {
            _ti.Format = format;
            return this;
        }
        public RTTBuilder SetWrapS(GLEnum wrapS)
        {
            _ti.WrapS = wrapS;
            return this;
        }
        public RTTBuilder SetWrapT(GLEnum wrapT)
        {
            _ti.WrapT = wrapT;
            return this;
        }
        public RTTBuilder SetMinFilter(GLEnum min)
        {
            _ti.MinFilter = min;
            return this;
        }
        public RTTBuilder SetMagFilter(GLEnum mag)
        {
            _ti.MagFilter= mag;
            return this;
        }
        public RTTBuilder SetResolutionMultiplier(Vector2 mul)
        {
            _ti.SizeMultiplier = mul;
            return this;
        }
        public RTTBuilder SetStatic(Vector2 size)
        {
            _ti.Size = size;
            _ti.FollowsWindowSize = false;
            return this;
        }
        public RenderTexture Build()
        {
            var tex = new GLTexture(GL, _ti);
            var rt = new RenderTexture(tex, _ti);
            return rt;
        }


    }


    public class RTBuilder
    {
        private RenderTargetInfo _ti = new();
        private RTManager _rtm;
        public RTBuilder(RTManager rtm)
        {
            _rtm = rtm;
        }
        public RTBuilder SetName(string name)
        {
            _ti.Name = name;
            return this;
        }
        public RTBuilder SetSize(Vector2 size)
        {
            _ti.Size = size;
            return this;
        }
        public RTBuilder AddTexture(RenderTexture tex)
        {
            _ti.Textures.Add(tex);
            return this;
        }

        public RTBuilder SetDepthBuffer(DepthBuffer db)
        {
            _ti.DepthBuffer = db;
            return this;
        }

        public RenderTarget Build()
        {
            return _rtm.CreateRenderTarget(_ti);
        }
    }
}
