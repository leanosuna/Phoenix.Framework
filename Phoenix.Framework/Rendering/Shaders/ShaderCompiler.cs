using Silk.NET.Shaderc;
using System.Security.Cryptography;
using System.Text;

namespace Phoenix.Framework.Rendering.Shaders;

/// <summary>
/// Compiles GLSL shader sources into SPIR-V bytecode at runtime with automatic disk caching and file watching.
/// </summary>
public static unsafe class ShaderCompiler
{
    private static readonly object _lock = new();

    /// <summary>
    /// Compiles GLSL source string directly into SPIR-V bytecode in memory.
    /// </summary>
    public static byte[] CompileGlsl(string source, ShaderKind kind, string fileName = "shader", bool optimize = true)
    {
        lock (_lock)
        {
            var shaderc = Shaderc.GetApi();
            Compiler* compiler = shaderc.CompilerInitialize();
            CompileOptions* options = shaderc.CompileOptionsInitialize();

            try
            {
                shaderc.CompileOptionsSetTargetEnv(options, TargetEnv.Vulkan, (uint)EnvVersion.Vulkan13);
                shaderc.CompileOptionsSetOptimizationLevel(options,
                    optimize ? OptimizationLevel.Performance : OptimizationLevel.Zero);

                var result = shaderc.CompileIntoSpv(compiler, source, (nuint)source.Length, kind, fileName, "main", options);
                var status = shaderc.ResultGetCompilationStatus(result);

                if (status != CompilationStatus.Success)
                {
                    string error = shaderc.ResultGetErrorMessageS(result);
                    shaderc.ResultRelease(result);
                    throw new InvalidOperationException($"Failed to compile shader '{fileName}' ({kind}): {error}");
                }

                nuint length = shaderc.ResultGetLength(result);
                byte* pBytes = shaderc.ResultGetBytes(result);
                byte[] bytecode = new byte[length];

                fixed (byte* dst = bytecode)
                {
                    Buffer.MemoryCopy(pBytes, dst, length, length);
                }

                shaderc.ResultRelease(result);
                return bytecode;
            }
            finally
            {
                shaderc.CompileOptionsRelease(options);
                shaderc.CompilerRelease(compiler);
            }
        }
    }

    /// <summary>
    /// Loads a shader from disk, checking for an up-to-date cached .spv binary before compiling.
    /// </summary>
    public static byte[] LoadOrCompile(string filePath, ShaderKind kind, string? cacheDirectory = null, bool optimize = true)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Shader source file not found: {filePath}", filePath);

        string source = File.ReadAllText(filePath);
        string hash = ComputeHash(source);

        string cacheDir = cacheDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), ".cache", "shaders");
        Directory.CreateDirectory(cacheDir);

        string baseName = Path.GetFileNameWithoutExtension(filePath);
        string cacheFile = Path.Combine(cacheDir, $"{baseName}_{kind}_{hash}.spv");

        if (File.Exists(cacheFile))
        {
            return File.ReadAllBytes(cacheFile);
        }

        byte[] bytecode = CompileGlsl(source, kind, Path.GetFileName(filePath), optimize);
        File.WriteAllBytes(cacheFile, bytecode);
        return bytecode;
    }

    /// <summary>
    /// Computes a deterministic SHA256 hex string for the input text.
    /// </summary>
    public static string ComputeHash(string text)
    {
        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hashBytes)[..16].ToLowerInvariant();
    }

    /// <summary>
    /// Watches a shader source file and invokes a callback whenever the file is modified on disk.
    /// </summary>
    public static FileSystemWatcher WatchShader(string filePath, Action onModified)
    {
        string? directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        string fileName = Path.GetFileName(filePath);

        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            throw new DirectoryNotFoundException($"Directory not found for shader: {filePath}");

        FileSystemWatcher watcher = new(directory, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true
        };

        DateTime lastRead = DateTime.MinValue;
        watcher.Changed += (_, _) =>
        {
            DateTime lastWriteTime = File.GetLastWriteTime(filePath);
            if (lastWriteTime - lastRead > TimeSpan.FromMilliseconds(200))
            {
                lastRead = lastWriteTime;
                onModified();
            }
        };

        return watcher;
    }
}
