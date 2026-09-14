using Phoenix.Framework.Rendering.Geometry.Model.Animations;
using Phoenix.Framework.Rendering.Vulkan;
using Silk.NET.Vulkan;
using System.Numerics;

namespace Phoenix.Framework.Rendering.Geometry.Model;

/// <summary>
/// Represents a skeletal animated 3D model with bone hierarchies, animation blending, and uniform buffer synchronization.
/// </summary>
public class AnimatedModel : Model
{
    public const int MaxBones = 128;

    private Graphics? _graphics;
    private DescriptorPool _boneDescriptorPool;
    private int _selectedAnimation;
    private Matrix4x4[] _blendFromMatrices = [];
    private Animation? _blendFromAnim;
    private float _blendFactor;
    private float _blendDuration = 0.25f;
    private bool _disposed;

    public List<Animation> Animations { get; internal set; } = [];
    public AnimatorNode[] AnimatorNodes { get; internal set; } = [];
    public Matrix4x4 InverseGlobalTransform { get; internal set; } = Matrix4x4.Identity;
    public int BoneCount { get; internal set; }
    public Matrix4x4[] FinalBoneMatrices { get; internal set; }

    public VulkanBuffer? BoneBuffer { get; private set; }
    public DescriptorSet BoneDescriptorSet { get; private set; }

    public AnimatedModel()
    {
        FinalBoneMatrices = new Matrix4x4[MaxBones];
        for (int i = 0; i < MaxBones; i++)
            FinalBoneMatrices[i] = Matrix4x4.Identity;
    }

    /// <summary>
    /// Allocates the bone uniform buffer and binds it to a Set 2 descriptor set via Graphics.
    /// </summary>
    public void InitializeGpuResources(Graphics graphics)
    {
        _graphics = graphics;

        if (FinalBoneMatrices.Length < MaxBones)
        {
            var resized = new Matrix4x4[MaxBones];
            Array.Copy(FinalBoneMatrices, resized, Math.Min(FinalBoneMatrices.Length, MaxBones));
            for (int i = FinalBoneMatrices.Length; i < MaxBones; i++)
                resized[i] = Matrix4x4.Identity;
            FinalBoneMatrices = resized;
        }

        ulong bufferSize = (ulong)(MaxBones * 64);
        BoneBuffer = graphics.CreateBuffer(
            bufferSize,
            BufferUsageFlags.UniformBufferBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

        BoneBuffer.SetData<Matrix4x4>(FinalBoneMatrices.AsSpan());

        var (pool, set) = graphics.CreateBoneDescriptorSet(BoneBuffer);
        _boneDescriptorPool = pool;
        BoneDescriptorSet = set;
    }

    /// <summary>
    /// Switches the active animation track.
    /// </summary>
    public void SetAnimation(int index)
    {
        if (index < 0 || index >= Animations.Count)
            throw new ArgumentOutOfRangeException(nameof(index), $"Animation index {index} out of range ({Animations.Count}).");

        _selectedAnimation = index;
        _blendFromAnim = null;
        _blendFactor = 0f;
    }

    /// <summary>
    /// Seeks the active animation to the specified timestamp in ticks.
    /// </summary>
    public void Seek(float time)
    {
        if (_selectedAnimation >= 0 && _selectedAnimation < Animations.Count)
        {
            Animations[_selectedAnimation].Seek(time);
        }
    }

    /// <summary>
    /// Smoothly transitions from the current animation pose to a target animation track over the specified duration.
    /// </summary>
    public void SetAnimationBlend(int index, float duration = 0.25f)
    {
        if (index < 0 || index >= Animations.Count)
            throw new ArgumentOutOfRangeException(nameof(index), $"Animation index {index} out of range ({Animations.Count}).");

        if (_selectedAnimation == index)
            return;

        _blendFromAnim = Animations[_selectedAnimation];
        if (_blendFromMatrices.Length != MaxBones)
            _blendFromMatrices = new Matrix4x4[MaxBones];

        Array.Copy(FinalBoneMatrices, _blendFromMatrices, MaxBones);

        _selectedAnimation = index;
        _blendFactor = 0f;
        _blendDuration = duration > 0 ? duration : 0.001f;
    }

    /// <summary>
    /// Evaluates skeletal bone keyframes, applies animation blending, and updates the GPU uniform buffer.
    /// </summary>
    public void Update(float deltaTime)
    {
        if (Animations.Count == 0 || _selectedAnimation < 0 || _selectedAnimation >= Animations.Count)
            return;

        var animation = Animations[_selectedAnimation];
        animation.Update(deltaTime, FinalBoneMatrices);

        if (_blendFromAnim != null)
        {
            _blendFactor += deltaTime / _blendDuration;
            if (_blendFactor >= 1.0f)
            {
                _blendFromAnim = null;
                _blendFactor = 0f;
            }
            else
            {
                int count = Math.Min(BoneCount, MaxBones);
                for (int b = 0; b < count; b++)
                {
                    FinalBoneMatrices[b] = Matrix4x4.Lerp(_blendFromMatrices[b], FinalBoneMatrices[b], _blendFactor);
                }
            }
        }

        BoneBuffer?.SetData<Matrix4x4>(FinalBoneMatrices.AsSpan());
    }

    /// <summary>
    /// Binds the bone descriptor set to Set 2 and renders all parts and submeshes.
    /// </summary>
    public void BindBoneDescriptorSet(RenderContext rc, VulkanPipeline pipeline, uint setIndex = 2)
    {
        if (BoneDescriptorSet.Handle != 0)
        {
            rc.BindDescriptorSet(pipeline, setIndex, BoneDescriptorSet);
        }
    }
    

    /// <summary>
    /// Disposes the bone GPU buffer, descriptor pool, and base model resources.
    /// </summary>
    public override void Dispose()
    {
        if (_disposed)
            return;

        BoneBuffer?.Dispose();
        BoneBuffer = null;

        if (_boneDescriptorPool.Handle != 0 && _graphics != null)
        {
            unsafe
            {
                _graphics.Context.Vk.DestroyDescriptorPool(_graphics.Context.Device, _boneDescriptorPool, null);
            }
            _boneDescriptorPool = default;
        }

        base.Dispose();
        _disposed = true;
    }
}
