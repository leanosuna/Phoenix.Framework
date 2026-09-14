using Phoenix.Framework.Rendering.Vulkan;
using System.Numerics;

namespace Phoenix.Framework.Rendering.Geometry.Model;

/// <summary>
/// Represents a single renderable submesh with GPU vertex and index buffers and local transformation.
/// </summary>
public sealed class ModelMesh : IDisposable
{
    private bool _disposed;

    public string Name { get; }
    public Matrix4x4 Transform { get; set; }
    public int MaterialIndex { get; set; }
    public int NormalIndex { get; set; } = -1;
    public uint VertexCount { get; }
    public uint IndexCount { get; }
    public VulkanBuffer VertexBuffer { get; }
    public VulkanBuffer IndexBuffer { get; }

    /// <summary>
    /// Initializes a ModelMesh with allocated GPU buffers, counts, and initial transformation.
    /// </summary>
    public ModelMesh(string name, VulkanBuffer vertexBuffer, VulkanBuffer indexBuffer, uint vertexCount, uint indexCount, Matrix4x4 transform, int materialIndex = 0, int normalIndex = -1)
    {
        Name = name;
        VertexBuffer = vertexBuffer;
        IndexBuffer = indexBuffer;
        VertexCount = vertexCount;
        IndexCount = indexCount;
        Transform = transform;
        MaterialIndex = materialIndex;
        NormalIndex = normalIndex;
    }

    /// <summary>
    /// Binds vertex and index buffers and issues an indexed draw call on the render context.
    /// </summary>
    public void Draw(RenderContext rc)
    {
        rc.BindVertexBuffer(VertexBuffer);
        rc.BindIndexBuffer(IndexBuffer);
        rc.DrawIndexed(IndexCount);
    }

    /// <summary>
    /// Disposes allocated vertex and index GPU buffers.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        VertexBuffer.Dispose();
        IndexBuffer.Dispose();
        _disposed = true;
    }
}
