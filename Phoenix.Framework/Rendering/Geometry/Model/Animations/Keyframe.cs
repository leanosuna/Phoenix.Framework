using System.Numerics;

namespace Phoenix.Framework.Rendering.Geometry.Model.Animations;

/// <summary>
/// A single animation keyframe specifying a timestamp and corresponding SRT transform.
/// </summary>
public class Keyframe
{
    public float TimeStamp { get; }
    public Transform SRT { get; internal set; }

    public Keyframe(float timeStamp, Vector3 scale, Quaternion rotation, Vector3 position)
    {
        TimeStamp = timeStamp;
        SRT = new Transform(scale, rotation, position);
    }

    public Keyframe(float timeStamp, Transform srt)
    {
        TimeStamp = timeStamp;
        SRT = srt;
    }

    /// <summary>
    /// Interpolates between this keyframe and the next keyframe.
    /// </summary>
    public Transform Interpolate(Keyframe next, float factor)
    {
        return SRT.Interpolate(next.SRT, factor);
    }
}
