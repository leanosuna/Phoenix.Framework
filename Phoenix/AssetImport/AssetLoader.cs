using Phoenix.Rendering.Geometry.Model;
using Phoenix.Rendering.Shaders;
using Phoenix.Rendering.Textures;
using Silk.NET.OpenGL;

namespace Phoenix.AssetImport
{
    public static class AssetLoader
    {
        public const string ContentDefaultPath = "Content/";
        public const string ManifestDefaultPath = ContentDefaultPath+"asset-manifest.json";
        public static string ContentBinBaseDirectory = "";

        private static readonly Dictionary<string, Model> _loadedModels = new();
        private static readonly Dictionary<string, GLTexture> _loadedTextures = new();
        private static readonly Dictionary<string, GLShader> _loadedShaders = new();
        private static AssetManifest _assetManifest = default!;
        private static GL GL = default!;
        public static void Init(PhoenixGame game, string contentPath = ContentDefaultPath, string manifestPath = ManifestDefaultPath)
        { 
            GL = game.GL;
            var manifestAbsPath = Path.Combine(AppContext.BaseDirectory, manifestPath);
            
            if(!JsonIOTools.Load(manifestAbsPath, out AssetManifest manifest))
                throw new Exception($"Manifest file not found at [{manifestAbsPath}]");
            
            _assetManifest = manifest;
            ContentBinBaseDirectory = contentPath+"ContentBin/";
        }
        
        public static Model LoadModel(string name)
        {
            var absolutePath = AssetAbsolutePath(name);
            if (!_loadedModels.TryGetValue(absolutePath, out var model))
            {
                model = BinaryModelReader.Load(GL, absolutePath);
                _loadedModels[absolutePath] = model;
            }

            return model;
        }
        public static GLTexture LoadTexture(string name)
        {
            var absolutePath = AssetAbsolutePath(name);
            return LoadTextureAbs(absolutePath);
        }
        public static GLShader LoadShader(string name)
        {
            var path = ShaderAbsolutePath(name);
            return LoadShaderAbs(path.absVert, path.absFrag);
        }

        private static GLTexture LoadTextureAbs(string absolutePath)
        {
            if (!_loadedTextures.TryGetValue(absolutePath, out var tex))
            {
                tex = BinaryTextureReader.Load(GL, absolutePath);
                _loadedTextures[absolutePath] = tex;
            }

            return tex;
        }

        private static GLShader LoadShaderAbs(string vert, string frag)
        {
            var name = Path.GetFileNameWithoutExtension(vert);

            if(!_loadedShaders.TryGetValue(name, out var shader))
            {
                shader = new GLShader(GL, vert, frag);
            }

            return shader;
        }

        private static string AssetAbsolutePath(string name)
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

        private static (string absVert, string absFrag) ShaderAbsolutePath(string name)
        {
            var fileName = Path.GetFileNameWithoutExtension(name);

            var assets = _assetManifest.Assets
                .FindAll(a =>
                    Path.GetFileNameWithoutExtension(a.RelativePath)
                    .Equals(fileName, StringComparison.OrdinalIgnoreCase)).ToList();

            if (assets.Count != 2)
                throw new Exception($"Expected 2 files for {name}, found {assets.Count}");


            var pathA = assets[0].RelativePath;
            var pathB = assets[1].RelativePath;


            var absolutePathA = Path.Combine(
               AppContext.BaseDirectory,
               ContentBinBaseDirectory,
               pathA
            ).Replace('\\', '/');
            var absolutePathB = Path.Combine(
               AppContext.BaseDirectory,
               ContentBinBaseDirectory,
               pathB
            ).Replace('\\', '/');


            return Path.GetExtension(pathA) == ".vert" ?
                (absolutePathA, absolutePathB) :
                (absolutePathB, absolutePathA);
        }

    }

}
