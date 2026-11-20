using System.Numerics;

namespace Phoenix.Framework.Rendering.Geometry.Model;

/// <summary>
/// Delegate that computes custom push constants for a model submesh given its draw context and world transformation matrix.
/// </summary>
public delegate TPush SubmeshPushConstantSelector<TPush>(in ModelDrawSubmesh submesh, in Matrix4x4 world) where TPush : unmanaged;
