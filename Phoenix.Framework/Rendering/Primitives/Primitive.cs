using Phoenix.Framework.Rendering.Geometry.Model;
using Phoenix.Framework.Rendering.Geometry.Vertices;
using Phoenix.Framework.Rendering.Vulkan;
using Silk.NET.Vulkan;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Phoenix.Framework.Rendering.Primitives;

/// <summary>
/// Base class for procedurally generated 3D geometric shapes with configurable vertex layouts and GPU mesh buffers.
/// </summary>
public abstract class Primitive : IDisposable
{
    private static Graphics? _graphics;
    protected PrimitiveInfo _primitiveInfo = null!;
    private ModelMesh? _mesh;
    private byte[] _vertexData = [];
    private uint[] _indexData = [];
    private VertexDeclaration _vertexDeclaration = null!;
    private bool _disposed;

    /// <summary>
    /// Configures the active graphics instance used to allocate GPU vertex and index buffers.
    /// </summary>
    public static void Initialize(Graphics graphics)
    {
        _graphics = graphics;
    }

    /// <summary>
    /// Gets the allocated ModelMesh handle if GPU resources were created.
    /// </summary>
    public ModelMesh? Mesh => _mesh;

    /// <summary>
    /// Binds vertex and index buffers and issues an indexed draw call into the active render pass.
    /// </summary>
    public void Draw(RenderContext rc)
    {
        _mesh?.Draw(rc);
    }

    /// <summary>
    /// Extracts typed vertex records from the generated primitive buffer.
    /// </summary>
    public T[] GetVertexData<T>() where T : unmanaged
    {
        return MemoryMarshal.Cast<byte, T>(_vertexData).ToArray();
    }

    /// <summary>
    /// Returns a copy of the primitive index buffer array.
    /// </summary>
    public uint[] GetIndexData()
    {
        return (uint[])_indexData.Clone();
    }

    /// <summary>
    /// Builds CPU vertex and index streams based on PrimitiveInfo and allocates Vulkan GPU buffers when Graphics is available.
    /// </summary>
    protected void BuildMesh()
    {
        var vdd = new VertexDeclarationBuilder().AddVertex3f();

        var uv = _primitiveInfo.Uv;
        var n = _primitiveInfo.Normals;
        var t = _primitiveInfo.Tangents;
        var bt = _primitiveInfo.Bitangents;

        if (_primitiveInfo.MeshPrimitiveType == PrimitiveTopology.LineList)
        {
            _vertexDeclaration = vdd.Build();
            var lineVbb = VertexBufferBuilder.BuildPos();
            uint[] lineIndices = [];
            VertexIndexBufferLines(ref lineVbb, ref lineIndices);
            _vertexData = lineVbb.Build();
            _indexData = lineIndices;
            UploadGpuBuffers();
            return;
        }

        if (uv)
            vdd.AddVertex2f();
        if (n)
            vdd.AddVertex3f();
        if (t)
            vdd.AddVertex3f();
        if (bt)
            vdd.AddVertex3f();

        _vertexDeclaration = vdd.Build();

        VertexBufferBuilder vbb = VertexBufferBuilder.BuildPos();
        uint[] indices = [];

        if (!uv && !n)
        {
            VertexIndexBufferPos(ref vbb, ref indices);
        }
        else if (uv && !n)
        {
            vbb = VertexBufferBuilder.BuildPosUv();
            VertexIndexBufferPosUv(ref vbb, ref indices);
        }
        else if (uv && n && !t && !bt)
        {
            vbb = VertexBufferBuilder.BuildPosUvNorm();
            VertexIndexBufferPosUvNorm(ref vbb, ref indices);
        }
        else if (uv && n && t && bt)
        {
            vbb = VertexBufferBuilder.BuildPosUvNormTanBt();
            VertexIndexBufferPosUvNormTaBt(ref vbb, ref indices);
        }
        else
        {
            throw new InvalidOperationException(
                $"Unsupported vertex layout: Uv={uv}, Normals={n}, Tangents={t}, Bitangents={bt}");
        }

        _vertexData = vbb.Build();
        _indexData = indices;

        if (_indexData.Length == 0)
            throw new InvalidOperationException("No indices generated for the requested vertex layout.");

        UploadGpuBuffers();
    }

    private void UploadGpuBuffers()
    {
        if (_graphics == null || _vertexData.Length == 0 || _indexData.Length == 0)
            return;

        var vtxBuffer = new VulkanBuffer(
            _graphics.Context,
            (ulong)_vertexData.Length,
            BufferUsageFlags.VertexBufferBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
        vtxBuffer.SetData<byte>(_vertexData);

        var idxBuffer = new VulkanBuffer(
            _graphics.Context,
            (ulong)(_indexData.Length * sizeof(uint)),
            BufferUsageFlags.IndexBufferBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
        idxBuffer.SetData<uint>(_indexData);

        uint vtxCount = (uint)(_vertexData.Length / _vertexDeclaration.StrideBytes);
        _mesh = new ModelMesh(GetType().Name, vtxBuffer, idxBuffer, vtxCount, (uint)_indexData.Length, Matrix4x4.Identity);
    }

    protected abstract void VertexIndexBufferPos(ref VertexBufferBuilder vbb, ref uint[] indices);
    protected abstract void VertexIndexBufferPosUv(ref VertexBufferBuilder vbb, ref uint[] indices);
    protected abstract void VertexIndexBufferPosUvNorm(ref VertexBufferBuilder vbb, ref uint[] indices);
    protected abstract void VertexIndexBufferPosUvNormTaBt(ref VertexBufferBuilder vbb, ref uint[] indices);
    protected abstract void VertexIndexBufferLines(ref VertexBufferBuilder vbb, ref uint[] indices);

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _mesh?.Dispose();
    }
}