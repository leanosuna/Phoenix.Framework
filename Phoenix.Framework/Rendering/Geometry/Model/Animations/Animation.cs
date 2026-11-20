using System.Numerics;

namespace Phoenix.Framework.Rendering.Geometry.Model.Animations;

/// <summary>
/// Represents a skeletal animation track consisting of per-bone precomputed keyframe sequences.
/// </summary>
public class Animation
{
    public string Name { get; private set; } = "";
    public float Duration { get; private set; }
    public float TicksPerSecond { get; private set; }
    public Keyframe[][] Keyframes { get; internal set; } = [];

    private float _currentTime;
    private int _boneCount;

    public Animation(string name, float duration, float tps, Keyframe[][] keyframes)
    {
        Name = name;
        Duration = duration;
        TicksPerSecond = tps <= 0 ? 25.0f : tps;
        Keyframes = keyframes;
        _boneCount = keyframes.Length;
    }

    /// <summary>
    /// Seeks the animation playback to a specific timestamp in ticks.
    /// </summary>
    public void Seek(float time)
    {
        _currentTime = Duration > 0 ? time % Duration : 0f;
    }

    /// <summary>
    /// Resets animation playback to time zero.
    /// </summary>
    public void Reset()
    {
        _currentTime = 0f;
    }

    /// <summary>
    /// Evaluates interpolated bone transformation matrices for the elapsed delta time.
    /// </summary>
    public void Update(float deltaTime, Matrix4x4[] finalBoneMatrices)
    {
        if (Duration <= 0 || Keyframes.Length == 0)
            return;

        _currentTime += TicksPerSecond * deltaTime;
        _currentTime %= Duration;

        int count = Math.Min(_boneCount, finalBoneMatrices.Length);
        for (int b = 0; b < count; b++)
        {
            var keys = Keyframes[b];
            if (keys.Length == 0)
            {
                finalBoneMatrices[b] = Matrix4x4.Identity;
                continue;
            }

            int i0 = GetStartingIndex(_currentTime, keys);
            int i1 = Math.Min(i0 + 1, keys.Length - 1);

            var interpolated = Interpolate(keys[i0], keys[i1], _currentTime);
            finalBoneMatrices[b] = interpolated.AsMatrix();
        }
    }

    private static int GetStartingIndex(float time, Keyframe[] keys)
    {
        if (keys.Length <= 1) return 0;
        int low = 0;
        int high = keys.Length - 2;
        int result = 0;

        while (low <= high)
        {
            int mid = low + (high - low) / 2;
            if (keys[mid].TimeStamp <= time)
            {
                result = mid;
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return result;
    }

    /// <summary>
    /// Binary searches a sorted keyframe list for the interval index containing the timestamp.
    /// </summary>
    public static int GetStartingIndex<T>(List<T> list, float time, Func<T, float> getTime)
    {
        if (list.Count <= 1) return 0;
        int low = 0;
        int high = list.Count - 2;
        int result = 0;

        while (low <= high)
        {
            int mid = low + (high - low) / 2;
            if (getTime(list[mid]) <= time)
            {
                result = mid;
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return result;
    }

    private Transform Interpolate(Keyframe current, Keyframe next, float time)
    {
        float lerpFactor = 0.0f;
        float midWayLength = time - current.TimeStamp;
        float framesDiff = next.TimeStamp - current.TimeStamp;
        if (framesDiff > 0)
            lerpFactor = Math.Clamp(midWayLength / framesDiff, 0f, 1f);

        return current.Interpolate(next, lerpFactor);
    }

    /// <summary>
    /// Precomputes animation frames across all timestamps, folding bone hierarchy matrices ahead of time.
    /// </summary>
    public void Precompute(IReadOnlyList<AnimatorNode> nodes, Matrix4x4 inverseGlobalTransform)
    {
        int boneCount = Keyframes.Length;
        var allTimestamps = new SortedSet<float>();
        for (int b = 0; b < boneCount; b++)
        {
            foreach (var kf in Keyframes[b])
                allTimestamps.Add(kf.TimeStamp);
        }

        var newKeyframes = new List<Keyframe>[boneCount];
        for (int b = 0; b < boneCount; b++)
            newKeyframes[b] = new List<Keyframe>();

        foreach (var timestamp in allTimestamps)
        {
            List<Keyframe> kfs = new(boneCount);
            for (int b = 0; b < boneCount; b++)
            {
                var localSRT = InterpolateLocalSRT(Keyframes[b], timestamp);
                kfs.Add(new Keyframe(timestamp, localSRT.Scale, localSRT.Rotation, localSRT.Translation));
            }

            ProcessFrame(nodes, inverseGlobalTransform, kfs);

            for (int b = 0; b < boneCount; b++)
            {
                newKeyframes[b].Add(kfs[b]);
            }
        }

        Keyframe[][] result = new Keyframe[boneCount][];
        for (int b = 0; b < boneCount; b++)
            result[b] = newKeyframes[b].ToArray();

        Keyframes = result;
        _boneCount = boneCount;
    }

    private void ProcessFrame(IReadOnlyList<AnimatorNode> nodes, Matrix4x4 inverseGlobalTransform, List<Keyframe> keyFrame)
    {
        int nodeCount = nodes.Count;
        for (int i = 0; i < nodeCount; i++)
        {
            var node = nodes[i];
            int pid = node.ParentID;
            Matrix4x4 parentTransform = pid != -1 ? nodes[pid].Transform : Matrix4x4.Identity;

            Matrix4x4 localTransform;
            if (node.IsBone && node.ModelBoneID < keyFrame.Count)
            {
                var animTransform = keyFrame[node.ModelBoneID].SRT.AsMatrix();
                animTransform = Matrix4x4.Transpose(animTransform);
                localTransform = animTransform;
            }
            else
            {
                localTransform = node.BindTransform;
            }

            node.Transform = parentTransform * localTransform;

            if (node.IsBone && node.ModelBoneID < keyFrame.Count)
            {
                Matrix4x4 final = inverseGlobalTransform * node.Transform * node.Offset;
                final = Matrix4x4.Transpose(final);
                if (Matrix4x4.Decompose(final, out var scale, out var rotation, out var translation))
                {
                    keyFrame[node.ModelBoneID].SRT = new Transform(scale, rotation, translation);
                }
            }
        }
    }

    private static Transform InterpolateLocalSRT(Keyframe[] keyframes, float time)
    {
        if (keyframes.Length == 0)
            return new Transform(Vector3.One, Quaternion.Identity, Vector3.Zero);

        if (keyframes.Length == 1)
            return keyframes[0].SRT;

        int i0 = GetStartingIndex(time, keyframes);
        int i1 = Math.Min(i0 + 1, keyframes.Length - 1);
        var k0 = keyframes[i0];
        var k1 = keyframes[i1];

        float diff = k1.TimeStamp - k0.TimeStamp;
        if (diff < 0.0001f)
            return k0.SRT;

        float factor = (time - k0.TimeStamp) / diff;
        return k0.SRT.Interpolate(k1.SRT, factor);
    }
}
