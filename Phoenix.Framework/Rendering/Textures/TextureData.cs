using System.Numerics;

namespace Phoenix.Framework.Rendering.Textures
{
    internal record TextureData
    {
        public string Name { get; set; } = "";
        public int WrapS { get; set; }
        public int WrapT { get; set; }
        public int FilterMin { get; set; }
        public int FilterMag { get; set; }
        public float Anisotropic { get; set; }
        public byte Format { get; set; }
        public int MipCount { get; set; }
        public Vector2[] MipSizes { get; set; } = [];
        public byte[][] EncodedBytes { get; set; } = [];

    }
}
