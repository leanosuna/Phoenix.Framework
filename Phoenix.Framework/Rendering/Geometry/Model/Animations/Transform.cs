using System.Numerics;
using System.Runtime.InteropServices;

namespace Phoenix.Framework.Rendering.Geometry.Model.Animations;

/// <summary>
/// Represents scale, rotation, and translation components of a bone or node transform.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Transform
{
    public Vector3 Scale;
    public Quaternion Rotation;
    public Vector3 Translation;

    public Transform(Vector3 scale, Quaternion rotation, Vector3 translation)
    {
        Scale = scale;
        Rotation = rotation;
        Translation = translation;
    }

    /// <summary>
    /// Linearly interpolates scale and translation, and spherically interpolates rotation.
    /// </summary>
    public Transform Interpolate(Transform other, float factor)
    {
        return new Transform(
            Vector3.Lerp(Scale, other.Scale, factor),
            Quaternion.Slerp(Rotation, other.Rotation, factor),
            Vector3.Lerp(Translation, other.Translation, factor));
    }

    /// <summary>
    /// Computes the 4x4 matrix representation of this SRT transform.
    /// </summary>
    public Matrix4x4 AsMatrix()
    {
        return Matrix4x4.CreateScale(Scale)
               * Matrix4x4.CreateFromQuaternion(Rotation)
               * Matrix4x4.CreateTranslation(Translation);
    }
}
