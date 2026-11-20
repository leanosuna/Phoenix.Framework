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
    private readonly VulkanBuffer?[] _boneBuffers = new VulkanBuffer?[VulkanSwapchain.MaxFramesInFlight];
    private readonly DescriptorSet[] _boneDescriptorSets = new DescriptorSet[VulkanSwapchain.MaxFramesInFlight];
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

    public VulkanBuffer? BoneBuffer => _graphics != null ? _boneBuffers[_graphics.Context.CurrentFrame] : _boneBuffers[0];
    public DescriptorSet BoneDescriptorSet => _graphics != null ? _boneDescriptorSets[_graphics.Context.CurrentFrame] : _boneDescriptorSets[0];

    public DescriptorSet GetBoneDescriptorSet(int frameIndex) => _boneDescriptorSets[frameIndex];
    public VulkanBuffer? GetBoneBuffer(int frameIndex) => _boneBuffers[frameIndex];

    public AnimatedModel()
    {
        FinalBoneMatrices = new Matrix4x4[MaxBones];
        for (int i = 0; i < MaxBones; i++)
            FinalBoneMatrices[i] = Matrix4x4.Identity;
    }

    /// <summary>
    /// Allocates the bone uniform buffers and binds them to Set 3 descriptor sets via Graphics.
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
        for (int i = 0; i < VulkanSwapchain.MaxFramesInFlight; i++)
        {
            var buf = graphics.CreateBuffer(
                bufferSize,
                BufferUsageFlags.UniformBufferBit,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

            buf.SetData<Matrix4x4>(FinalBoneMatrices.AsSpan());
            _boneBuffers[i] = buf;

            var (_, set) = graphics.CreateBoneDescriptorSet(buf);
            _boneDescriptorSets[i] = set;
        }
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

        UploadBoneTransforms();
    }

    /// <summary>
    /// Uploads current bone matrices to the active frame's GPU uniform buffer.
    /// </summary>
    public void UploadBoneTransforms()
    {
        if (_graphics != null)
        {
            _boneBuffers[_graphics.Context.CurrentFrame]?.SetData<Matrix4x4>(FinalBoneMatrices.AsSpan());
        }
    }

    /// <summary>
    /// Binds the bone descriptor set to Set 3 for skeletal vertex skinning, ensuring current bone transforms are uploaded for the render frame.
    /// </summary>
    public void BindBoneDescriptorSet(RenderContext rc, VulkanPipeline pipeline, uint setIndex = 3)
    {
        int frame = rc.CurrentFrame;
        _boneBuffers[frame]?.SetData<Matrix4x4>(FinalBoneMatrices.AsSpan());
        var set = _boneDescriptorSets[frame];
        if (set.Handle != 0)
        {
            rc.BindDescriptorSet(pipeline, setIndex, set);
        }
    }

    /// <summary>
    /// Disposes all bone GPU buffers, frees descriptor sets, and disposes base model resources.
    /// </summary>
    public override void Dispose()
    {
        if (_disposed)
            return;

        for (int i = 0; i < _boneBuffers.Length; i++)
        {
            _boneBuffers[i]?.Dispose();
            _boneBuffers[i] = null;

            if (_boneDescriptorSets[i].Handle != 0 && _graphics != null)
            {
                _graphics.FreeBoneDescriptorSet(_boneDescriptorSets[i]);
                _boneDescriptorSets[i] = default;
            }
        }

        base.Dispose();
        _disposed = true;
    }
}
