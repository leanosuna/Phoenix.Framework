using System.Numerics;

namespace Phoenix.Framework.AssetImport.Processing;

/// <summary>
/// Intermediate node for building and folding the bone and transformation hierarchy before animator baking.
/// </summary>
internal sealed class ModelBoneHierarchyNode
{
    public string Name { get; }
    public Matrix4x4 Transform { get; }
    public bool IsBone { get; }
    public Matrix4x4 Offset { get; }
    public List<ModelBoneHierarchyNode> Children { get; }

    public ModelBoneHierarchyNode(string name, Matrix4x4 transform, List<ModelBoneHierarchyNode> children)
    {
        Name = name;
        Transform = transform;
        Children = children;
        IsBone = false;
        Offset = Matrix4x4.Identity;
    }

    public ModelBoneHierarchyNode(string name, Matrix4x4 transform, List<ModelBoneHierarchyNode> children, Matrix4x4 offset)
    {
        Name = name;
        Transform = transform;
        Children = children;
        IsBone = true;
        Offset = offset;
    }
}
