using Silk.NET.Assimp;

namespace Phoenix.Framework.AssetImport;

/// <summary>
/// Configuration options for runtime 3D model importing and post-processing.
/// </summary>
public sealed class ModelLoadOptions
{
    private static readonly Dictionary<string, PostProcessSteps> _flagAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CalculateTangentSpace"] = PostProcessSteps.CalculateTangentSpace,
        ["CalcTangentSpace"] = PostProcessSteps.CalculateTangentSpace,
        ["GenerateSmoothNormals"] = PostProcessSteps.GenerateSmoothNormals,
        ["GenSmoothNormals"] = PostProcessSteps.GenerateSmoothNormals,
        ["GenerateNormals"] = PostProcessSteps.GenerateNormals,
        ["GenNormals"] = PostProcessSteps.GenerateNormals,
        ["GenerateUVCoords"] = PostProcessSteps.GenerateUVCoords,
        ["GenUVCoords"] = PostProcessSteps.GenerateUVCoords,
        ["PreTransform"] = PostProcessSteps.PreTransformVertices,
        ["PreTransformVertices"] = PostProcessSteps.PreTransformVertices,
        ["JoinIdenticalVertices"] = PostProcessSteps.JoinIdenticalVertices,
        ["Triangulate"] = PostProcessSteps.Triangulate,
        ["FlipUVs"] = PostProcessSteps.FlipUVs,
        ["FindInvalidData"] = PostProcessSteps.FindInvalidData,
        ["ImproveCacheLocality"] = PostProcessSteps.ImproveCacheLocality,
        ["SortByPrimitiveType"] = PostProcessSteps.SortByPrimitiveType,
        ["LimitBoneWeights"] = PostProcessSteps.LimitBoneWeights
    };

    public static readonly IReadOnlyList<string> DefaultAssimpFlags =
    [
        "Triangulate",
        "GenerateSmoothNormals",
        "GenerateUVCoords",
        "FindInvalidData",
        "FlipUVs",
        "JoinIdenticalVertices",
        "ImproveCacheLocality",
        "SortByPrimitiveType",
        "LimitBoneWeights",
        "CalculateTangentSpace"
    ];

    public bool ExtractTextures { get; set; } = false;
    public TextureCompressionFormat ExtractedTextureCompression { get; set; } = TextureCompressionFormat.BC7;
    public bool IsAnimated { get; set; } = false;
    public bool Tangents { get; set; } = true;
    public bool PreTransform { get; set; } = false;
    public float Scale { get; set; } = 1.0f;
    public List<string> AnimationFiles { get; set; } = [];

    public ModelLoadOptions Clone() => new()
    {
        ExtractTextures = ExtractTextures,
        ExtractedTextureCompression = ExtractedTextureCompression,
        IsAnimated = IsAnimated,
        Tangents = Tangents,
        PreTransform = PreTransform,
        Scale = Scale,
        AnimationFiles = [.. AnimationFiles],
        AssimpFlags = [.. AssimpFlags]
    };

    /// <summary>
    /// List of Assimp post-processing flag names to apply during import.
    /// In JSON, this is specified as an array of strings, e.g. ["Triangulate", "GenerateSmoothNormals", "FlipUVs"].
    /// </summary>
    public List<string> AssimpFlags { get; set; } = [.. DefaultAssimpFlags];

    /// <summary>
    /// Gets the combined PostProcessSteps bitmask calculated from the AssimpFlags string list.
    /// </summary>
    public uint AssimpFlagsMask => GetAssimpFlagsMask();

    /// <summary>
    /// Computes the combined PostProcessSteps bitmask from the configured AssimpFlags list.
    /// </summary>
    public uint GetAssimpFlagsMask()
    {
        if (AssimpFlags == null || AssimpFlags.Count == 0)
            return 0;

        uint mask = 0;
        foreach (var raw in AssimpFlags)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            string token = raw.Trim();

            if (token.Equals("default", StringComparison.OrdinalIgnoreCase))
            {
                mask |= GetDefaultMask();
                continue;
            }

            if (token.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                mask = 0;
                continue;
            }

            char op = '+';
            string name = token;
            if (token.StartsWith('+') || token.StartsWith('-'))
            {
                op = token[0];
                name = token[1..].Trim();
            }

            if (TryParseStep(name, out var step))
            {
                if (op == '+')
                    mask |= (uint)step;
                else
                    mask &= ~(uint)step;
            }
        }

        return mask;
    }

    /// <summary>
    /// Sets the AssimpFlags string list from a PostProcessSteps bitmask.
    /// </summary>
    public void SetAssimpFlags(PostProcessSteps steps)
    {
        AssimpFlags = new List<string>();
        foreach (PostProcessSteps step in Enum.GetValues<PostProcessSteps>())
        {
            if (step != 0 && (steps & step) == step)
            {
                AssimpFlags.Add(step.ToString());
            }
        }
    }

    /// <summary>
    /// Sets the AssimpFlags string list from a raw uint bitmask.
    /// </summary>
    public void SetAssimpFlags(uint mask) => SetAssimpFlags((PostProcessSteps)mask);

    private static bool TryParseStep(string name, out PostProcessSteps step)
    {
        if (_flagAliases.TryGetValue(name, out step))
            return true;

        if (Enum.TryParse(name, ignoreCase: true, out step) && Enum.IsDefined(step))
            return true;

        return false;
    }

    private static uint GetDefaultMask() => (uint)(
        PostProcessSteps.Triangulate |
        PostProcessSteps.GenerateSmoothNormals |
        PostProcessSteps.GenerateUVCoords |
        PostProcessSteps.FindInvalidData |
        PostProcessSteps.FlipUVs |
        PostProcessSteps.JoinIdenticalVertices |
        PostProcessSteps.ImproveCacheLocality |
        PostProcessSteps.SortByPrimitiveType |
        PostProcessSteps.LimitBoneWeights |
        PostProcessSteps.CalculateTangentSpace);
}
