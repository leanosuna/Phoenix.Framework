using Phoenix.Framework.Rendering.Vulkan;
using System.Numerics;

namespace Phoenix.Framework.Rendering.Geometry.Model;

/// <summary>
/// Container for 3D model parts, meshes, textures, and rendering dispatch.
/// Supports asynchronous texture streaming, per-mesh material index manipulation, and bindless slot tracking.
/// </summary>
public class Model : IDisposable
{
    private readonly object _textureLock = new();
    private int _loadedTexturesCount;
    private bool _disposed;

    public bool IsDisposed => _disposed;
    public List<ModelPart> Parts { get; internal set; } = [];
    public List<string> TextureNames { get; internal set; } = [];
    public List<VulkanTexture> Textures { get; internal set; } = [];
    public bool HasTextures => Textures.Count > 0 || TextureNames.Count > 0;

    public int TotalTextures { get; internal set; }
    public int LoadedTexturesCount => _loadedTexturesCount;
    public bool IsFullyLoaded => LoadedTexturesCount >= TotalTextures;
    public uint BaseTextureSlot { get; internal set; }
    public uint TextureSlotCount { get; internal set; }
    internal BindlessManager? BindlessManager { get; set; }

    public event Action? OnAllTexturesLoaded;
    public event Action<int, VulkanTexture>? OnTextureLoaded;

    /// <summary>
    /// Gets all flattened meshes across all model parts.
    /// </summary>
    public IReadOnlyList<ModelMesh> Submeshes
    {
        get
        {
            var list = new List<ModelMesh>();
            for (int p = 0; p < Parts.Count; p++)
            {
                list.AddRange(Parts[p].Meshes);
            }
            return list;
        }
    }

    /// <summary>
    /// Draws all parts and meshes of the model using the bound pipeline and provided world transform.
    /// </summary>
    public virtual void Draw(RenderContext rc, VulkanPipeline pipeline, in Matrix4x4 world)
    {
        Draw(rc, pipeline, in world, 0);
    }

    /// <summary>
    /// Draws all parts and meshes of the model using the bound pipeline, provided world transform, and optional custom flags.
    /// </summary>
    public virtual void Draw(RenderContext rc, VulkanPipeline pipeline, in Matrix4x4 world, uint customFlags)
    {
        rc.BindPipeline(pipeline);
        for (int p = 0; p < Parts.Count; p++)
        {
            var part = Parts[p];
            for (int m = 0; m < part.Meshes.Count; m++)
            {
                var mesh = part.Meshes[m];
                var meshWorld = mesh.Transform * world;

                uint albedo = 0;
                if (mesh.MaterialIndex >= 0 && mesh.MaterialIndex < Textures.Count && Textures[mesh.MaterialIndex] != null)
                {
                    albedo = Textures[mesh.MaterialIndex].TextureId;
                }
                else if (BaseTextureSlot != 0 && mesh.MaterialIndex >= 0 && (uint)mesh.MaterialIndex < TextureSlotCount)
                {
                    albedo = BaseTextureSlot + (uint)mesh.MaterialIndex;
                }
                else if (BindlessManager?.DefaultWhiteTexture != null)
                {
                    albedo = BindlessManager.DefaultWhiteTexture.TextureId;
                }

                uint normal = 0;
                if (mesh.NormalIndex >= 0 && mesh.NormalIndex < Textures.Count && Textures[mesh.NormalIndex] != null)
                {
                    normal = Textures[mesh.NormalIndex].TextureId;
                }
                else if (BaseTextureSlot != 0 && mesh.NormalIndex >= 0 && (uint)mesh.NormalIndex < TextureSlotCount)
                {
                    normal = BaseTextureSlot + (uint)mesh.NormalIndex;
                }

                PushConstantsData pc = new(meshWorld, albedoIndex: albedo, normalIndex: normal, customFlags: customFlags);
                rc.PushConstants(pipeline, pc);
                mesh.Draw(rc);
            }
        }
    }

    /// <summary>
    /// Draws all parts and meshes using a custom push constants selector for each submesh.
    /// </summary>
    public void Draw<TPush>(RenderContext rc, VulkanPipeline pipeline, in Matrix4x4 world, SubmeshPushConstantSelector<TPush> pushSelector) where TPush : unmanaged
    {
        rc.BindPipeline(pipeline);

        int submeshIndex = 0;
        for (int p = 0; p < Parts.Count; p++)
        {
            var part = Parts[p];
            for (int m = 0; m < part.Meshes.Count; m++)
            {
                var mesh = part.Meshes[m];
                var meshWorld = mesh.Transform * world;
                Matrix4x4 normalMatrix = Matrix4x4.Identity;
                if (Matrix4x4.Invert(meshWorld, out var inv))
                {
                    normalMatrix = Matrix4x4.Transpose(inv);
                }

                uint albedo = 0;
                if (mesh.MaterialIndex >= 0 && mesh.MaterialIndex < Textures.Count && Textures[mesh.MaterialIndex] != null)
                {
                    albedo = Textures[mesh.MaterialIndex].TextureId;
                }
                else if (BaseTextureSlot != 0 && mesh.MaterialIndex >= 0 && (uint)mesh.MaterialIndex < TextureSlotCount)
                {
                    albedo = BaseTextureSlot + (uint)mesh.MaterialIndex;
                }
                else if (BindlessManager?.DefaultWhiteTexture != null)
                {
                    albedo = BindlessManager.DefaultWhiteTexture.TextureId;
                }

                uint normal = 0;
                if (mesh.NormalIndex >= 0 && mesh.NormalIndex < Textures.Count && Textures[mesh.NormalIndex] != null)
                {
                    normal = Textures[mesh.NormalIndex].TextureId;
                }
                else if (BaseTextureSlot != 0 && mesh.NormalIndex >= 0 && (uint)mesh.NormalIndex < TextureSlotCount)
                {
                    normal = BaseTextureSlot + (uint)mesh.NormalIndex;
                }

                var info = new ModelDrawSubmesh
                {
                    SubmeshIndex = submeshIndex++,
                    Submesh = mesh,
                    Part = part,
                    WorldMatrix = meshWorld,
                    NormalMatrix = normalMatrix,
                    AlbedoTextureId = albedo,
                    NormalTextureId = normal,
                    MetallicRoughnessTextureId = 0,
                    EmissiveTextureId = 0,
                    OcclusionTextureId = 0
                };

                TPush push = pushSelector(in info, in world);
                rc.PushConstants(pipeline, in push);
                rc.BindVertexBuffer(mesh.VertexBuffer);
                rc.BindIndexBuffer(mesh.IndexBuffer);
                rc.DrawIndexed(mesh.IndexCount);
            }
        }
    }

    /// <summary>
    /// Draws all parts and meshes using a simplified custom push constants selector for each submesh.
    /// </summary>
    public void Draw<TPush>(RenderContext rc, VulkanPipeline pipeline, in Matrix4x4 world, Func<ModelDrawSubmesh, TPush> pushSelector) where TPush : unmanaged
    {
        Draw(rc, pipeline, in world, (in ModelDrawSubmesh sub, in Matrix4x4 _) => pushSelector(sub));
    }

    /// <summary>
    /// Draws all parts and meshes allowing custom per-mesh state setup prior to each draw call.
    /// </summary>
    public virtual void Draw(RenderContext rc, VulkanPipeline pipeline, in Matrix4x4 world, Action<RenderContext, ModelMesh, Matrix4x4> perMesh)
    {
        for (int p = 0; p < Parts.Count; p++)
        {
            var part = Parts[p];
            for (int m = 0; m < part.Meshes.Count; m++)
            {
                var mesh = part.Meshes[m];
                var meshWorld = mesh.Transform * world;
                perMesh(rc, mesh, meshWorld);
                mesh.Draw(rc);
            }
        }
    }

    /// <summary>
    /// Sets the material index on the specified part and mesh.
    /// </summary>
    public void SetMaterialIndex(int partIndex, int meshIndex, int materialIndex)
    {
        if (partIndex >= 0 && partIndex < Parts.Count && meshIndex >= 0 && meshIndex < Parts[partIndex].Meshes.Count)
        {
            Parts[partIndex].Meshes[meshIndex].MaterialIndex = materialIndex;
        }
    }

    /// <summary>
    /// Sets the material index on all meshes matching the specified name.
    /// </summary>
    public void SetMaterialIndex(string meshName, int materialIndex)
    {
        for (int p = 0; p < Parts.Count; p++)
        {
            var part = Parts[p];
            for (int m = 0; m < part.Meshes.Count; m++)
            {
                if (string.Equals(part.Meshes[m].Name, meshName, StringComparison.OrdinalIgnoreCase))
                {
                    part.Meshes[m].MaterialIndex = materialIndex;
                }
            }
        }
    }

    /// <summary>
    /// Sets the material index on the global sequential mesh index across all parts.
    /// </summary>
    public void SetMaterialIndex(int globalMeshIndex, int materialIndex)
    {
        int currentIndex = 0;
        for (int p = 0; p < Parts.Count; p++)
        {
            var part = Parts[p];
            for (int m = 0; m < part.Meshes.Count; m++)
            {
                if (currentIndex == globalMeshIndex)
                {
                    part.Meshes[m].MaterialIndex = materialIndex;
                    return;
                }
                currentIndex++;
            }
        }
    }

    /// <summary>
    /// Gets the first mesh matching the specified name, or null if not found.
    /// </summary>
    public ModelMesh? GetMesh(string meshName)
    {
        for (int p = 0; p < Parts.Count; p++)
        {
            var part = Parts[p];
            for (int m = 0; m < part.Meshes.Count; m++)
            {
                if (string.Equals(part.Meshes[m].Name, meshName, StringComparison.OrdinalIgnoreCase))
                    return part.Meshes[m];
            }
        }
        return null;
    }

    /// <summary>
    /// Gets the mesh at the specified part and mesh indices, or null if out of range.
    /// </summary>
    public ModelMesh? GetMesh(int partIndex, int meshIndex)
    {
        if (partIndex >= 0 && partIndex < Parts.Count && meshIndex >= 0 && meshIndex < Parts[partIndex].Meshes.Count)
        {
            return Parts[partIndex].Meshes[meshIndex];
        }
        return null;
    }

    /// <summary>
    /// Gets the mesh at the sequential global mesh index across all parts, or null if out of range.
    /// </summary>
    public ModelMesh? GetMesh(int globalMeshIndex)
    {
        int currentIndex = 0;
        for (int p = 0; p < Parts.Count; p++)
        {
            var part = Parts[p];
            for (int m = 0; m < part.Meshes.Count; m++)
            {
                if (currentIndex == globalMeshIndex)
                    return part.Meshes[m];
                currentIndex++;
            }
        }
        return null;
    }

    /// <summary>
    /// Assigns or replaces the texture at the specified material index.
    /// </summary>
    public void SetTexture(int materialIndex, VulkanTexture texture)
    {
        lock (_textureLock)
        {
            while (Textures.Count <= materialIndex)
            {
                Textures.Add(null!);
            }
            Textures[materialIndex] = texture;
        }
    }

    internal void NotifyTextureLoaded(int index, VulkanTexture texture)
    {
        if (_disposed)
        {
            texture?.Dispose();
            return;
        }

        if (texture != null)
        {
            SetTexture(index, texture);
        }

        int loaded = Interlocked.Increment(ref _loadedTexturesCount);
        OnTextureLoaded?.Invoke(index, texture!);
        if (loaded >= TotalTextures)
        {
            OnAllTexturesLoaded?.Invoke();
        }
    }

    /// <summary>
    /// Disposes all model parts, meshes, associated textures, and reserved bindless slots.
    /// </summary>
    public virtual void Dispose()
    {
        if (_disposed)
            return;

        if (BaseTextureSlot != 0 && TextureSlotCount != 0 && BindlessManager != null)
        {
            BindlessManager.FreeSlots(BaseTextureSlot, TextureSlotCount);
            BaseTextureSlot = 0;
            TextureSlotCount = 0;
        }

        for (int i = 0; i < Parts.Count; i++)
        {
            Parts[i].Dispose();
        }
        Parts.Clear();

        lock (_textureLock)
        {
            for (int i = 0; i < Textures.Count; i++)
            {
                Textures[i]?.Dispose();
            }
            Textures.Clear();
        }

        _disposed = true;
    }
}
