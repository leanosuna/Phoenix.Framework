using Phoenix.AssetImport.Model;
using Phoenix.AssetImport.Texture;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.AssetImport
{
    public static class AssetLoader
    {
        public const string ManifestDefaultPath = "Content/asset-manifest.json";
        public static string ContentBinBaseDirectory = "";

        private static readonly Dictionary<string, BinaryModel> _loadedModels = new();
        private static readonly Dictionary<string, BinaryTexture> _loadedTextures = new();
        //private static readonly Dictionary<string, BinaryShader> _loadedShaders = new();

        private static AssetManifest _assetManifest = default!;
        internal static GL GL = default!;
        public static void Init(GL gl, string path = ManifestDefaultPath)
        {
            GL=gl;
            var manifestPath = Path.Combine(AppContext.BaseDirectory, path);
            _assetManifest = AssetManifestIO.Load(manifestPath);
            ContentBinBaseDirectory = "Content/ContentBin/";
        }
        internal static string AssetAbsolutePath(string name)
        {
            var noExt = Path.ChangeExtension(name, null).Replace('\\', '/');

            var relativeBin = Path.ChangeExtension(name, ".bin");
            
            var asset = _assetManifest.Assets
                .Find(a => 
                    Path.ChangeExtension(a.RelativePath, null)
                    .Replace('\\', '/')
                    .Equals(noExt, StringComparison.OrdinalIgnoreCase));

            if (asset == null)
                throw new Exception($"Asset '{name}' not found in manifest");

            var absolutePath = Path.Combine(
                AppContext.BaseDirectory,
                ContentBinBaseDirectory,
                relativeBin
            ).Replace('\\', '/');

            return absolutePath;
        }

        public static BinaryModel LoadModel(string name)
        {
            var absolutePath = AssetAbsolutePath(name);
            if (!_loadedModels.TryGetValue(absolutePath, out var model))
            {
                model = BinaryModelReader.Load(absolutePath);
                _loadedModels[absolutePath] = model;
            }

            return model;
        }
        public static BinaryTexture LoadTextureAbs(string absolutePath)
        {
            if (!_loadedTextures.TryGetValue(absolutePath, out var tex))
            {
                tex = BinaryTextureReader.Load(absolutePath);
                _loadedTextures[absolutePath] = tex;
            }

            return tex;
        }

        public static BinaryTexture LoadTexture(string name)
        {
            var absolutePath = AssetAbsolutePath(name);
            return LoadTextureAbs(absolutePath);
        }

    }

}
