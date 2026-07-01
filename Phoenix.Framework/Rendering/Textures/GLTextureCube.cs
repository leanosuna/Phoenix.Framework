using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.Framework.Rendering.Textures
{
    public class GLTextureCube
    {
        public uint Handle;

        //public byte Format;

        private GL GL;
        private List<TextureData> _data;
        internal GLTextureCube(GL gl, List<TextureData> data)
        {
            GL = gl;
            //Format = (InternalFormat)data.First().Format;

            Handle = GL.GenTexture();

            _data = data;
            Bind();
            LoadIntoGL();
            Bind();
            LoadParametersIntoGL();
        }

        public void Bind(TextureUnit textureSlot = TextureUnit.Texture0)
        {
            GL.ActiveTexture(textureSlot);
            GL.BindTexture(GLEnum.TextureCubeMap, Handle);
        }

        private void LoadIntoGL()
        {
            for(var i = 0; i < 6; i++)
            {
                var td = _data[i];
                var size = td.MipSizes[0];
                
                int internalFormat = td.Format switch
                {
                    0 => GLTexture.GL_RGBA8,
                    1 => GLTexture.GL_COMPRESSED_RGBA_S3TC_DXT1_EXT, // BC1
                    2 => GLTexture.GL_COMPRESSED_RGBA_S3TC_DXT5_EXT, // BC3
                    3 => GLTexture.GL_COMPRESSED_RG_RGTC2,          // BC5
                    _ => throw new Exception("Unknown texture format")
                };

                for (int mip = 0; mip < td.MipCount; mip++)
                {
                    var w = (uint)td.MipSizes[mip].X;
                    var h = (uint)td.MipSizes[mip].Y;
                    byte[] data = td.EncodedBytes[mip];

                    if (td.Format == 0) // RGBA8
                    {
                        unsafe
                        {
                            fixed (byte* ptr = data)
                            {
                                GL.TexImage2D(
                                    GLEnum.TextureCubeMapPositiveX + i,
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
                        }
                    }
                    else // BC1 / BC3 / BC5
                    {
                        unsafe
                        {
                            fixed (byte* ptr = data)
                            {
                                GL.CompressedTexImage2D(
                                    GLEnum.TextureCubeMapPositiveX + i,
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

        }

        private void LoadParametersIntoGL()
        {
            GL.TexParameter(TextureTarget.TextureCubeMap, GLEnum.TextureWrapS, (int)GLEnum.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, GLEnum.TextureWrapT, (int)GLEnum.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, GLEnum.TextureMinFilter, (int)GLEnum.Linear);
            GL.TexParameter(TextureTarget.TextureCubeMap, GLEnum.TextureMagFilter, (int)GLEnum.Linear);

        }
    }
}
