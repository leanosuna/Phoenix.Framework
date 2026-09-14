using System.Numerics;

namespace Phoenix.Framework.AssetImport.Processing;

/// <summary>
/// Intermediate bone descriptor containing the assigned bone index and inverse bind pose offset matrix.
/// </summary>
internal sealed class BoneInfo
{
    public int ID { get; }
    public Matrix4x4 Offset { get; }

    public BoneInfo(int id, Matrix4x4 offset)
    {
        ID = id;
        Offset = offset;
    }
}
