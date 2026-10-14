using System.Runtime.InteropServices;

namespace Phoenix.Framework.Rendering.Geometry.Indirect;

/// <summary>
/// Represents the GPU indirect draw arguments matching the native VkDrawIndexedIndirectCommand struct layout.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct DrawIndexedIndirectCommand
{
    public uint IndexCount;
    public uint InstanceCount;
    public uint FirstIndex;
    public int VertexOffset;
    public uint FirstInstance;

    public DrawIndexedIndirectCommand(
        uint indexCount,
        uint instanceCount,
        uint firstIndex,
        int vertexOffset,
        uint firstInstance)
    {
        IndexCount = indexCount;
        InstanceCount = instanceCount;
        FirstIndex = firstIndex;
        VertexOffset = vertexOffset;
        FirstInstance = firstInstance;
    }
}
