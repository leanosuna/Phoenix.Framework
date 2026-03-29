using Phoenix.Rendering.Textures;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Phoenix.Rendering.Textures
{
    internal static class BinaryTextureReader
    {
        public static GLTexture Load(GL gl, string path)
        {
            using var fs = File.OpenRead(path);
            using var br = new BinaryReader(fs);

            var assetType = br.ReadString();
            var ver = br.ReadUInt32();

            var wrapS = br.ReadInt32();
            var wrapT = br.ReadInt32();
            var fMin = br.ReadInt32();
            var fMag = br.ReadInt32();

            var format = br.ReadByte();
            var mipCount = br.ReadInt32();

            var encodedBytes = new byte[mipCount][];
            var mipSizes = new Vector2[mipCount];
            for (int i = 0; i < mipCount; i++)
            {
                var mipW = br.ReadInt32();
                var mipH = br.ReadInt32();
                mipSizes[i] = new Vector2(mipW, mipH);

                var bufferLenth = br.ReadInt32();
                encodedBytes[i] = new byte[bufferLenth];
                encodedBytes[i] = br.ReadBytes(bufferLenth);
            }
            var name = Path.GetFileNameWithoutExtension(path);

            return new GLTexture(gl, name, wrapS, wrapT, fMin, fMag, format, mipCount, mipSizes, encodedBytes);
        }
    }
}
