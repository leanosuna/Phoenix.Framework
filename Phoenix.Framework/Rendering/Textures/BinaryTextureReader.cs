using Silk.NET.OpenGL;
using System.Numerics;

namespace Phoenix.Framework.Rendering.Textures
{
    internal static class BinaryTextureReader
    {
        public static GLTexture Load(GL gl, string path)
        {
            return new GLTexture(gl, InternalLoad(gl, path));
        }

        public static GLTextureCube LoadCube(GL gl, string[] path)
        {
            
            var data = path.Select(p => InternalLoad(gl, p)).ToList();

            return new GLTextureCube(gl, data);
        }

        internal static TextureData InternalLoad(GL gl, string path)
        {
            using var fs = File.OpenRead(path);
            using var br = new BinaryReader(fs);

            var assetType = br.ReadString();
            var ver = br.ReadUInt32();

            var wrapS = br.ReadInt32();
            var wrapT = br.ReadInt32();
            var fMin = br.ReadInt32();
            var fMag = br.ReadInt32();
            var anisotropic = br.ReadSingle();
            var format = br.ReadByte();
            var mipCount = br.ReadInt32();

            var mipSizes = new Vector2[mipCount];
            var encodedBytes = new byte[mipCount][];
            for (int i = 0; i < mipCount; i++)
            {
                var mipW = br.ReadInt32();
                var mipH = br.ReadInt32();
                mipSizes[i] = new Vector2(mipW, mipH);

                var bufferLength = br.ReadInt32();
                encodedBytes[i] = br.ReadBytes(bufferLength);
            }

            return new TextureData
            {
                WrapS = wrapS,
                WrapT = wrapT,
                FilterMin = fMin,
                FilterMag = fMag,
                Anisotropic = anisotropic,
                Format = format,
                MipCount = mipCount,
                MipSizes = mipSizes,
                EncodedBytes = encodedBytes,
                Name = Path.GetFileNameWithoutExtension(path)
            };

        }

        
    }
}
