using Phoenix.Rendering.RT;
using Silk.NET.OpenGL;
using System.Numerics;
using System.Runtime.Intrinsics.X86;

namespace Phoenix.Rendering.Textures
{
    public class GLTexture
    {
        const int GL_RGBA8 = 0x8058;
        const int GL_COMPRESSED_RGBA_S3TC_DXT1_EXT = 0x83F1; // BC1
        const int GL_COMPRESSED_RGBA_S3TC_DXT5_EXT = 0x83F3; // BC3
        const int GL_COMPRESSED_RG_RGTC2 = 0x8DBD; // BC5

        public string Name { get; private set; } = default!;
        public uint Handle { get; private set; } = default!;
        public Vector2 Size { get; private set;  } = default!;
        public int WrapS { get; private set; } = default!;
        public int WrapT { get; private set; } = default!;
        public int FilterMin { get; private set; } = default!;
        public int FilterMag { get; private set; } = default!;

        public byte Format { get; private set; } = default!;
        public InternalFormat InternalFormat { get; private set; } = default!;
        public int MipCount { get; private set; } = 1;
        public Vector2[] MipSizes { get; private set; } = default!;
        public byte[][] EncodedBytes { get; private set; } = default!;

        private GL GL;

        public GLTexture(GL gl, string name, int wrapS, int wrapT, int fMin, int fMag, 
            byte format, int mipCount, Vector2[] mipSizes, byte[][] encodedBytes)
        {
            GL = gl;
            Name = name;
            WrapS = wrapS;
            WrapT = wrapT;
            FilterMin = fMin;
            FilterMag = fMag;
            Format = format;
            MipCount = mipCount;
            MipSizes = mipSizes;
            EncodedBytes = encodedBytes;
            Handle = GL.GenTexture();
            Bind();
            LoadIntoGL();
            LoadParametersIntoGL();
        }

        public GLTexture(GL gl, RenderTextureInfo info)
        {
            GL = gl;
            WrapS = (int)info.WrapS;
            WrapT = (int)info.WrapT;
            FilterMin = (int)info.MinFilter;
            FilterMag = (int)info.MagFilter;
            InternalFormat = info.Format;

            Handle = GL.GenTexture();
            //Bind();
        }

        public void Bind(TextureUnit textureSlot = TextureUnit.Texture0)
        {
            GL.ActiveTexture(textureSlot);
            GL.BindTexture(TextureTarget.Texture2D, Handle);
        }
        private void LoadIntoGL()
        {   
            Size = MipSizes[0];
            
            int internalFormat = Format switch
            {
                0 => GL_RGBA8,
                1 => GL_COMPRESSED_RGBA_S3TC_DXT1_EXT, // BC1
                2 => GL_COMPRESSED_RGBA_S3TC_DXT5_EXT, // BC3
                3 => GL_COMPRESSED_RG_RGTC2,          // BC5
                _ => throw new Exception("Unknown texture format")
            };

            for (int mip = 0; mip < MipCount; mip++)
            {
                var w = (uint)MipSizes[mip].X;
                var h = (uint)MipSizes[mip].Y;
                byte[] data = EncodedBytes[mip];

                if (Format == 0) // RGBA8
                {
                    unsafe
                    {
                        fixed (byte* ptr = data)
                        {
                            TexImage(mip, internalFormat, w, h, ptr);
                        }
                    }
                }
                else // BC1 / BC3 / BC5
                {
                    unsafe
                    {
                        fixed (byte* ptr = data)
                        {
                            GL.CompressedTexImage2D(
                                TextureTarget.Texture2D,
                                mip,
                                (InternalFormat)internalFormat,
                                w,
                                h,
                                0,
                                (uint)data.Length,
                                ptr
                            );
                        }
                    }
                }
            }

            
        }
        public unsafe void Resize(Vector2 size)
        {
            Bind();

            Size = size;
            
            TexImage(0, (int)InternalFormat, (uint)Size.X, (uint)Size.Y, null);
            LoadParametersIntoGL();
        }

        private unsafe void TexImage(int mip, int internalFormat, uint w, uint h, byte * ptr)
        {
            GL.TexImage2D(
                GLEnum.Texture2D,
                mip,
                internalFormat,
                w,
                h,
                0,
                GLEnum.Rgba,
                GLEnum.UnsignedByte,
                ptr
            );
        }

        
        private void LoadParametersIntoGL()
        {
            GL.TexParameter(TextureTarget.Texture2D, GLEnum.TextureWrapS, WrapS);
            GL.TexParameter(TextureTarget.Texture2D, GLEnum.TextureWrapT, WrapT);
            GL.TexParameter(TextureTarget.Texture2D, GLEnum.TextureMinFilter, FilterMin);
            GL.TexParameter(TextureTarget.Texture2D, GLEnum.TextureMagFilter, FilterMag);

            GL.TexParameter(TextureTarget.Texture2D, GLEnum.TextureBaseLevel, 0);
            GL.TexParameter(TextureTarget.Texture2D, GLEnum.TextureMaxLevel, MipCount - 1);
        }
    }
}
